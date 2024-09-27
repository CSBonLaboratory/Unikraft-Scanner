import subprocess
from colorama import Fore
import yaml
from CoveritySetup import CoveritySetup
from tool_configs.default.Default_1 import Default_1
import os.path
from os import listdir
import pymongo
import re

def get_firefox(firefoxPath : str) -> int:

    p = subprocess.Popen(f"wget https://ftp.mozilla.org/pub/firefox/releases/112.0/linux-x86_64/en-US/firefox-112.0.tar.bz2 -P {firefoxPath}", shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    _, err = p.communicate()

    if p.returncode != 0:
        print(f"{Fore.RED} Error when downloading firefox: {err} {Fore.RESET}")
        p.terminate()
        return p.returncode

    p.terminate()

    p = subprocess.Popen(f"tar -xf {os.path.join(firefoxPath, 'firefox-112.0.tar.bz2')} -C {firefoxPath}", shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    _, err = p.communicate()

    if p.returncode != 0:
        print(f"{Fore.RED} Error when unziping firefox: {err} {Fore.RESET}")
        p.terminate()
        return p.returncode

    p.terminate()

    p = subprocess.Popen(f"rm {os.path.join(firefoxPath, 'firefox-112.0.tar.bz2')}", shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    _, err = p.communicate()

    if p.returncode != 0:
        print(f"{Fore.RED} Error when deleting the firefox archive: {err} {Fore.RESET}")
        p.terminate()
        return p.returncode

    p.terminate()

    return 0



def get_gecko(geckoPath : str) -> int:

    p = subprocess.Popen(f"wget https://github.com/mozilla/geckodriver/releases/download/v0.34.0/geckodriver-v0.34.0-linux64.tar.gz -P {geckoPath}", shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    _, err = p.communicate()

    if p.returncode != 0:
        print(f"{Fore.RED} Error when downloading gecko: {err}")
        p.terminate()
        return p.returncode

    p.terminate()

    p = subprocess.Popen(f"tar -xf {os.path.join(geckoPath, 'geckodriver-v0.34.0-linux64.tar.gz')} -C {geckoPath}", shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    _, err = p.communicate()

    if p.returncode != 0:
        print(f"{Fore.RED} Error when unziping gecko: {err}")
        p.terminate()
        return p.returncode

    p.terminate()

    p = subprocess.Popen(f"rm {os.path.join(geckoPath, 'geckodriver-v0.34.0-linux64.tar.gz')}", shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    _, err = p.communicate()

    if p.returncode != 0:
        print(f"{Fore.RED} Error when deleting the gecko archive: {err}")
        p.terminate()
        return p.returncode

    p.terminate()
    
    return 0


def version_1(config_context : dict):

    print(f"{Fore.BLUE} Configuring basic options {Fore.RESET}")

    default : Default_1 = Default_1()

    logfile = input(f"Where log file should be: [{os.path.abspath(default.logfile)}]: ")
    if logfile ==  '':
        config_context['logfile'] = os.path.abspath(default.logfile)
    else:
        config_context['logfile'] = logfile
    
    outfile = input(f"Where the output file should be [{os.path.abspath(default.outfile)}]: ")
    if outfile == '':
        config_context['outfile'] = os.path.abspath(default.outfile)
    else:
        config_context['outfile'] = outfile

    config_context['preprocFile'] = default.preprocFile

    verbose = input(f"Level of verbosity for logging: 1 - DEBUG, 2 - INFO, 3 - WARNING, 4 - ERROR, 5 - CRITICAL, [{default.verbose}]: ")

    if verbose == "":
        config_context['verbose'] = default.verbose
    else:
        config_context['verbose'] = verbose

    print(f"{Fore.BLUE} Configuring options for Coverity Scan integration {Fore.RESET}")
    
    config_context['coverityAPI'] = {}
    config_context['coverityAPI']['userEmail'] = input("Email used for creating a Coverity Scan account. Same email will be used for working with the compilation coverage DB: ")

    projectName = input(f"Name of the Coverity project in which you can view all analysis attempts. It should be the title of the project from the Overview landing page in Coverity [{default.coverityProjectName}]: ")
    if projectName == '':
        config_context['coverityAPI']['projectName'] = default.coverityProjectName
    else:
        config_context['coverityAPI']['projectName'] = projectName

    projecOverview = input(f"Coverity project overview page URL [{default.coverityProjectOverviewURL}]: ")
    if projecOverview == '':
        config_context['coverityAPI']['projectOverviewURL'] = default.coverityProjectOverviewURL
    else:
        config_context['coverityAPI']['projectOverviewURL'] = projecOverview
    
    firefoxPath = input(f"Destination directory where a non-snap Firefox binary will be unziped and installed [{os.path.abspath(default.firefoxPath)}]: ")
    
    if firefoxPath == "":
        config_context['coverityAPI']['firefoxPath'] = os.path.abspath(default.firefoxPath)
    else:
        config_context['coverityAPI']['firefoxPath'] = os.path.abspath(firefoxPath)

    get_firefox(config_context['coverityAPI']['firefoxPath'])
    config_context['coverityAPI']['firefoxPath'] = os.path.join(config_context['coverityAPI']['firefoxPath'], "firefox/firefox")
    print(f"{Fore.YELLOW} Final firefox path is to executable: {config_context['coverityAPI']['firefoxPath']} {Fore.RESET}")

    geckoPath = input(f"Destination directory where the Gecko webdriver will be unziped and installed [{os.path.abspath(default.geckoPath)}]: ")
    
    if geckoPath == "":
        config_context['coverityAPI']['geckoPath'] = os.path.abspath(default.geckoPath)
    else:
        config_context['coverityAPI']['geckoPath'] = os.path.abspath(geckoPath)

    get_gecko(config_context['coverityAPI']['geckoPath'])
    config_context['coverityAPI']['geckoPath'] = os.path.join(config_context['coverityAPI']['geckoPath'], "geckodriver")
    print(f"{Fore.YELLOW} Final gecko path is to executable: {config_context['coverityAPI']['geckoPath']} {Fore.RESET}")

    covSuitepath = input(f"Path where the Coverity build tools will be unziped and installed [{os.path.abspath(default.covSuitePath)}]: ")

    if covSuitepath == "":
        config_context["coverityAPI"]['covSuitePath'] = os.path.abspath(default.covSuitePath)
    else:
        config_context["coverityAPI"]['covSuitePath'] = os.path.abspath(covSuitepath)

   
    extern_directories = [f for f in listdir(config_context["coverityAPI"]['covSuitePath']) if not os.path.isfile(os.path.join(config_context["coverityAPI"]['covSuitePath'], f))]
    max_coverity_version = ""
    print(extern_directories)
    for f in extern_directories:
        cov_match = re.match(r"cov-analysis-linux64-(\d+)\.(\d+).(\d+)", f)
        if cov_match != None and os.path.isfile(os.path.join(config_context["coverityAPI"]['covSuitePath'], f, "bin", "cov-build")):
            print(f'{Fore.YELLOW} Found potential Coverity build tools suite at: {os.path.join(config_context["coverityAPI"]["covSuitePath"], f)} {Fore.RESET}')
            potential_coverity_dir = os.path.join(os.path.join(config_context["coverityAPI"]['covSuitePath'], f))
            if potential_coverity_dir > max_coverity_version:
                max_coverity_version = potential_coverity_dir
    
    if max_coverity_version == "":
        print(f'{Fore.RED} No coverity build tools suite unziped at {config_context["coverityAPI"]["covSuitePath"]} {Fore.RESET}')
        exit(0)
    else:
        config_context["coverityAPI"]['covSuitePath'] = os.path.join(max_coverity_version, "bin" , "cov-build")

    print(f"{Fore.BLUE}Opening a browser to scrape the Coverity upload token and install Coverity suite.\nEnter your Coverity account credentials, solve the captcha and LET THE SCRAPER DO ITS WORK !{Fore.RESET}")

    config_context['coverityAPI']['scraperWaitSeconds'] = default.scraperWaitSeconds
    
    config_context['coverityAPI']['interceptorTimeoutSeconds'] = default.interceptorTimeoutSeconds

    config_context['coverityAPI']['snapshotPollingSeconds'] = default.snapshotPollingSeconds

    config_context['coverityAPI']['recentSnapshotRetries'] = default.recentSnapshotsRetries
    
    coveritySetup : CoveritySetup = CoveritySetup(config_context)

    config_context['coverityAPI']['uploadToken'] = coveritySetup.get_upload_token()

    print(f"{Fore.YELLOW}Got upload token: {config_context['coverityAPI']['uploadToken']} {Fore.RESET}")

    print(f"{Fore.BLUE} Configuring client for Unikraft scanning coverage DB {Fore.RESET}")

    config_context['unikraftCoverageDB'] = {}

    dbPass = input("Password given by the admin, the first time, for authentication to the DB. It will be changed in the next steps: ")

    dbIp = input("IP given by the admin for the coverage statistics DB: ")
    config_context['unikraftCoverageDB']['ip'] = dbIp

    dbPort = int(input(f"Port given by the admin for the coverage statistics DB: "))
    config_context['unikraftCoverageDB']['port'] = dbPort

    config_context['unikraftCoverageDB']['user'] = config_context['coverityAPI']['userEmail']
    client = pymongo.MongoClient(
            host=dbIp,
            username=config_context['unikraftCoverageDB']['user'],
            password=dbPass,
            port=config_context['unikraftCoverageDB']['port'],
            authSource='admin',
            authMechanism='SCRAM-SHA-1'
    )
    
    first_pass = dbPass
    copy_pass = ""

    while first_pass != copy_pass: 

        first_pass = input("Enter the new password: ")
        copy_pass = input("Type the password again: ")

        if first_pass != copy_pass:
            print("Passwords are different. Please try again.")
    
    
    config_context['unikraftCoverageDB']['pass'] = first_pass

    client['admin'].command("updateUser", config_context['unikraftCoverageDB']['user'], pwd=config_context['unikraftCoverageDB']['pass'])

    client.close()

    print(config_context['unikraftCoverageDB']['port'])

    print(f"{Fore.YELLOW} New password has been configured. {Fore.RESET}")

    try:
        client = pymongo.MongoClient(
                host=dbIp,
                username=config_context['unikraftCoverageDB']['user'],
                password=config_context['unikraftCoverageDB']['pass'],
                port=config_context['unikraftCoverageDB']['port'],
                authSource='admin',
                authMechanism='SCRAM-SHA-1'
        )

        print(client['admin'].list_collection_names())

    except Exception as e:
        print(f"{Fore.RED} Password change failed: {e} {Fore.RESET}")
        exit(0)

    print(f"{Fore.GREEN} New authentication OK {Fore.RESET}")
    
    client.close()
    return config_context
    

def setup_tool(version : int, config_path : str):

    config_context = {}

    print(f"Start configuration based on schema version {version}")
    config_context['version'] = version

    if version == 1:
        version_1(config_context)

    with open(config_path, 'w') as outfile:
        yaml.safe_dump(config_context, outfile)

    print(f"{Fore.BLUE} Cofiguration with schema version {version} written to {config_path}. If any future configurations must be changed, manually modify the configuration file instead of using the setup command {Fore.RESET}")
    