from selenium.webdriver.firefox.options import Options
from selenium.webdriver.firefox.service import Service
from seleniumwire import webdriver
from seleniumwire.webdriver import Firefox
from selenium.webdriver.support.wait import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.by import By
from selenium.common.exceptions import TimeoutException

class CoveritySetup:
    
    projectOverviewURL : str

    browser : Firefox
    def __new__(cls, configs : dict | None) -> None:
        if not hasattr(cls, 'instance') and configs != None:
            cls.instance : CoveritySetup = super(CoveritySetup, cls).__new__(cls)
            cls.instance.options = Options()

            cls.instance.firefoxPath = configs['coverityAPI']['firefoxPath']

            cls.instance.options.binary_location = configs["coverityAPI"]["firefoxPath"]

            cls.instance.projectOverviewURL = configs["coverityAPI"]["projectOverviewURL"]

            cls.instance.covSuitePath = configs["coverityAPI"]['covSuitePath']

            cls.instance.options.set_preference("browser.download.dir", configs["coverityAPI"]['covSuitePath'])
            cls.instance.options.set_preference("browser.download.manager.showWhenStarting", True)
            cls.instance.options.set_preference("browser.helperApps.neverAsk.saveToDisk", "application/octet-stream")
            cls.instance.options.set_preference("browser.download.folderList", 2)

            gecko_service = Service(configs["coverityAPI"]["geckoPath"])
            cls.instance.browser = webdriver.Firefox(options=cls.instance.options, service=gecko_service)

            cls.instance.scraper_wait = configs["coverityAPI"]['scraperWaitSeconds']
        
        return cls.instance


    def get_upload_token(self) -> str:
        
        browser : Firefox = self.browser

        browser.get(self.projectOverviewURL)

        try:
            # wait until "View Defects" button can be seen and clicked. This means that we wait until the user MANUALLY logs in and solves the captcha
            submit_build_btn = WebDriverWait(browser, self.scraper_wait).until(EC.element_to_be_clickable((By.XPATH, "/html/body/div[1]/div[2]/div/div/div[3]/div[1]/p[2]/a")))
            submit_build_btn.click()
        except TimeoutException as te:
            raise te

        try:
            WebDriverWait(browser, self.scraper_wait).until(EC.visibility_of_any_elements_located((By.XPATH, "/html/body/div[1]/div[2]/div/div/div[4]/ol/li[1]/pre/span[1]")))

            upload_token = browser.find_element(By.XPATH, "/html/body/div[1]/div[2]/div/div/div[4]/ol/li[1]/pre/span[1]").text

        except TimeoutException as te:
            raise te
        
        return upload_token

        
        