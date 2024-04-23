#from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.wait import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.firefox.options import Options
from selenium.webdriver.firefox.service import Service
from selenium.webdriver import ActionChains
from seleniumwire import webdriver
from seleniumwire.webdriver import Firefox
from seleniumwire.request import Request, Response
from helpers.InterceptorTimeout import InterceptorTimeout
from selenium.common.exceptions import TimeoutException
import os
import json
import logging
import subprocess
from threading import Condition
from enum import Enum


logger = logging.getLogger(__name__)

#TODO: remove logs generated by Selenium

class RecentSnapshotStates(Enum):
    NOT_STARTED = 0
    STARTED_BUT_NOT_FOUND = 1
    FOUND = 2

class DefectsStates(Enum):
    NOT_STARTED = 0
    STARTED_BUT_NOT_FOUND = 1
    FOUND = 2


def dummy_request_interceptor(request : Request):
    return

def recent_snapshot_response_interceptor(request : Request, response : Response):
    coverityAPI = CoverityAPI()

    if coverityAPI.recent_snapshot_state in [RecentSnapshotStates.NOT_STARTED, RecentSnapshotStates.FOUND]:
        logger.debug(f"RECENT SNAPSHOT: Ignoring {request.url} for recent snapshot since state is {coverityAPI.recent_snapshot_state.name}")
        return

    if request.url == coverityAPI.snapshots_table_url:
        with coverityAPI.interceptor_condition:
            
            snapshot_payload = json.loads(response.body.decode('utf-8'))['resultSet']['results']

            if snapshot_payload == []:
                coverityAPI.cached_recent_snapshot = None
            else:
                coverityAPI.cached_recent_snapshot = snapshot_payload[0]

            logger.debug(f"Found most recent snapshot {coverityAPI.cached_recent_snapshot}")

            coverityAPI.recent_snapshot_state = RecentSnapshotStates.FOUND

            coverityAPI.interceptor_condition.notify()

    return

def defects_response_interceptor(request : Request, response : Response):

    coverityAPI = CoverityAPI()

    if coverityAPI.defects_state in [DefectsStates.NOT_STARTED, DefectsStates.FOUND]:
        logger.debug(f"DEFECTS: Ignoring {request.url} for recent defects since status is {coverityAPI.defects_state.name}")
        return
    
    if request.url == coverityAPI.defects_table_url:
        with coverityAPI.interceptor_condition:

            coverityAPI.cached_last_defect_results = json.loads(response.body.decode('utf-8'))['resultSet']['results']

            logger.debug(f"Found last defects {coverityAPI.cached_last_defect_results}")

            coverityAPI.defects_state = DefectsStates.FOUND

            coverityAPI.interceptor_condition.notify()
    
    return

class CoverityAPI(object):

    user_email : str
    user_pass : str
    upload_token : str
    browser : Firefox
    scraper_wait : int
    project_overview_url : str
    project_id : int
    snapshots_url : str
    snapshots_table_url : str
    snapshots_view_id : int
    defects_url : str
    defects_table_url : str
    defects_view_id : int
    recent_snapshot_state : RecentSnapshotStates
    defects_state : DefectsStates
    interceptor_condition : Condition
    cached_last_defect_results : list[dict]
    cached_recent_snapshot : dict
    interceptor_timeout : int
    is_auth : bool

    def __new__(cls, configs : dict | None) -> None:
        if not hasattr(cls, 'instance'):
            
            cls.instance : CoverityAPI = super(CoverityAPI, cls).__new__(cls)

            cls.instance.interceptor_condition = Condition()

            cls.instance.browser.request_interceptor = dummy_request_interceptor

            cls.instance.recent_snapshot_state = RecentSnapshotStates.NOT_STARTED
            cls.instance.defects_state = DefectsStates.NOT_STARTED

            cls.instance.snapshots_view_ids = configs["CoverityAPI"]["snapshotsViewId"]
            cls.instance.project_id = configs["CoverityAPI"]["projectId"]
            cls.instance.defects_view_id = configs["CoverityAPI"]["defectsViewId"]

            # this url is accessible and will trigger a request to the second one which contains snapshots data
            cls.instance.snapshots_url = f"https://scan9.scan.coverity.com/#/project-view/{cls.instance.snapshots_view_id}/{cls.instance.project_id}"
            cls.instance.snapshots_table_url = f"https://scan9.scan.coverity.com/reports/table.json\?projectId={cls.instance.project_id}&viewId={cls.instance.snapshots_view_id}"

            # this url is accessible and will trigger a request to the second one which contains defects data for the selected snapshot
            cls.instance.defects_url = f"https://scan9.scan.coverity.com/#/project-view/{cls.instance.defects_view_id}/{cls.instance.project_id}"
            cls.instance.defects_table_url = f"https://scan9.scan.coverity.com/reports/table.json\?projectId={cls.instance.project_id}&viewId={cls.instance.snapshots_view_id}"

            cls.instance.options.binary_location = configs["CoverityAPI"]["firefoxPath"]
            cls.instance.options = Options()
            gecko_service = Service(configs["CoverityAPI"]["geckoPath"])
            cls.instance.browser = webdriver.Firefox(options=cls.instance.options, service=gecko_service)

            cls.instance.scraper_wait = configs["CoverityAPI"]["scraperWaitSeconds"]
            cls.instance.project_overview_url = configs["CoverityAPI"]["projectOverviewURL"]

            cls.instance.interceptor_timeout = configs["CoverityAPI"]["interceptorTimeoutSeconds"]

            cls.instance.is_auth = False

        return cls.instance
    
    def auth(self):

        browser = self.browser

        browser.switch_to.new_window('tab')

        browser.get(self.project_overview_url)

        try:
            # wait until "View Defects" button can be seen and clicked. This means that we wait until the user MANUALLY logs in and solves the captcha
            WebDriverWait(browser, self.scraper_wait).until(EC.element_to_be_clickable((By.XPATH, "/html/body/div[1]/div[2]/div/div/div[3]/div[1]/p[1]/a")))
        except TimeoutException as te:
            self.is_auth = False
            raise te

        browser.close()
        
        self.is_auth = True

        return

    def prepare_defects_for_db(self, defect : dict) -> dict:

        # just remove the / from the beggining of the path, Coverity adds its since the archive submited is considered to be the whole filesystem

        defect['displayFile'] = defect['displayFile'][1 : ]

        return defect
    
    def check_recent_snapshot(self, compilation_tag : str) -> bool:

        if not self.is_auth:
            logger.warning("Authenticating before checking the most recent snapshot")
            try:
                self.auth()
            except TimeoutException as te:
                logger.critical("Authentication failed during fetching the most recent snapshot")
                raise te

        browser = self.browser
        
        # try to intercept response body which contains all snapshot information
        self.recent_snapshot_state = RecentSnapshotStates.STARTED_BUT_NOT_FOUND
        browser.response_interceptor = recent_snapshot_response_interceptor
        
        browser.switch_to.new_window('tab')
        
        browser.get(self.snapshots_url)

        # wait until the interceptor finds the response that contains the snapshot details
        with self.interceptor_condition:
            while self.recent_snapshot_state != RecentSnapshotStates.FOUND:
                self.interceptor_condition.wait(self.interceptor_timeout)

                # timeout inside the interceptor
                if self.recent_snapshot_state != RecentSnapshotStates.FOUND:
                    logger.critical(f"Interceptor timeout. Expected {RecentSnapshotStates.FOUND.name}, got {self.recent_snapshot_state.name}")
                    raise InterceptorTimeout()
                
            # reinit the state of the snapshots interceptor maybe for further use
            self.recent_snapshot_state = RecentSnapshotStates.NOT_STARTED

        # close the snapshot menu tab opened in this method
        browser.close()

        if self.cached_recent_snapshot['snapshotDescription'] != compilation_tag:
            logger.warning(f"Recent snapshot has cimpilation tag {self.cached_recent_snapshot['snapshotDescription']}, expected {compilation_tag}. Please retry.")
            return False
        
        return True

    def fetch_and_cache_recent_defects(self) -> None:
        
        if not self.is_auth:
            logger.warn("Authenticating before fetching most recent defects...")
            try:
                self.auth()
            except TimeoutException as te:
                logger.critical("Authentication failed during fetching defects in the most recent snapshot")
                raise te
            
        browser = self.browser

        # try to intercept response body which contains all snapshot information
        self.defects_state = DefectsStates.STARTED_BUT_NOT_FOUND
        browser.response_interceptor = defects_response_interceptor

        browser.switch_to.new_window('tab')

        browser.get(self.snapshots_url)

        try:
            recent_snapshot_cell = WebDriverWait(browser, self.scraper_wait).until(EC.element_to_be_clickable((By.XPATH, '/html/body/cim-root/cim-shell/div/cim-project-view/div[1]/div[1]/div[1]/cim-data-table/angular-slickgrid/div/div/div[4]/div[3]/div/div[1]/div[2]')))
            double_click_snapshot_cell = ActionChains(browser).double_click(recent_snapshot_cell).perform()
        except TimeoutException as te:
            logger.critical("Cannot find most recent snapshot cell when fetching defects")
            raise te

        with self.interceptor_condition:
            while self.defects_state != DefectsStates.FOUND:
                self.interceptor_condition.wait(self.interceptor_timeout)

                # timeout inside the interceptor
                if self.recent_snapshot_state != DefectsStates.FOUND:
                    logger.critical(f"Interceptor timeout. Expected {DefectsStates.FOUND.name}, got {self.recent_snapshot_state.name}")
                    raise InterceptorTimeout()
                
            # reinit the state of the defects interceptor maybe for further use
            self.recent_snapshot_state = DefectsStates.NOT_STARTED
        
        # close the snapshot menu tab opened in this method
        browser.close()

        return


    def submit_build(self, app_path : str, compile_cmd : str, pipeline : bool) -> bool:
        
        upload_script_path = os.getcwd() + "/.."

        # make this env vars in order to pass them to the upload.sh script
        os.environ['UK_COV_APP_COMPILE_CMD'] = compile_cmd
        os.environ['UK_COV_APP_PATH'] = app_path

        job = f"{upload_script_path}/upload.sh"

        # pipeline means that the environment does not have the coverity build tools, so we need to download them beforehand
        if pipeline == True:
            os.environ['UK_COV_MODE'] = "--pipeline"

        # this is for running this tool on a development environment where the Coverity build tools exist
        else:
            os.environ['UK_COV_MODE'] = "--local"

        submit_proc = subprocess.Popen(job, shell=True, stdout=subprocess.PIPE, env=os.environ)

        out, err = submit_proc.communicate()
        
        submit_proc.terminate()

        if err != "" or err != None:
            return False
        
        logger.info(out)
        logger.warning(err)
        return True
                    
