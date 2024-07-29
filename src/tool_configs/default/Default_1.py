
class Default_1:
  
  version : int = 1

  logfile : str = "./unikraft_scanner.log"

  preprocFile : str = "./.unikraft_scanner_preproc.log"

  outfile : str = "./coverage_out"

  verbose : int = 1

  coverityProjectName : str = "Unikraft-Scanning"

  coverityProjectOverviewURL : str = "https://scan.coverity.com/projects/unikraft-scanning?tab=overview"

  firefoxPath : str = "./extern"

  geckoPath : str = "./extern"

  covSuitePath : str = "./extern"

  scraperWaitSeconds : int = 300

  interceptorTimeoutSeconds : int = 300

  snapshotPollingSeconds : int = 60

  recentSnapshotsRetries : int = 15