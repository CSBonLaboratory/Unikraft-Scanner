# Unikraft Scanner 

Unikraft Scanner is a tool intended for developers, that solves issues regarding the scanning coverage when using third-party static code analysis security tools on the Unikraft repo.  

Static code analysis tools, such as Coverity or CodeQL, intercept the compilation stage of the target project, extract various information about the compiled code, build a database out of different compiler generated low-level representations and find possible vulnerabilities by querying the database for known weakness patterns. **Thus, only compiled code will be analyzed.** 

However, Unikraft is made of multiple loosely-coupled C modules that can be chosen to be compiled, or not, depending on the top-level application's requirements, ported to run over the Unikraft unikernel. This means that the Unikraft code repo is not a single compilation target but multiple "mutations" that can have missing modules that are not critically required by the application. Such modules are represented by 1 or more C source files compiled into a single object file. 

Unfortunately, besides entire C source files that may not be compiled in the current iteration, a C source file regarded by the build system to be compiled is not guaranteed to have been fully compiled. There may be statements such as `#if`, `#elif`, `#else`, `#ifndef`, `#ifdef` that rely on various configuration symbols that can deny their inner code to be compiled.  

In order to fully scan the Unikraft codebase we need to trigger compilation of all core modules, in an incremental way (multiple retries where we configure differently the unikernel for novel "mutations"). Such practice is shown in the image below.

![Alt text](docs/compile_cov.png)

# Core Concepts

Right now, the Unikraft Scanner system is divided in 3 environments:

- the local environments (in green): 
    1. Developers utilize the Unikraft Scanner CLI client to find the compilation coverage of a Unikraft app in regards to the whole Unikraft core, built using a local Unikraft repo or using the [Unikraft Applications & Examples Catalog](https://github.com/unikraft/catalog)
    2. CLI client starts the **Coverity local build suite**. These programs are given for free by Synopsys and intercept the compilation phase, build a database archive with low-level compiler representations of the code and send it to the Coverity Scan cloud for analysis.
    3. While the static analysis is running, the Unikraft Scanner CLI client instruments all `#if`, `#elif`, `#else`, `#ifndef`, `#ifdef` blocks within all C source files compiled at step 2, with a `#warning` directive to see which block is triggered at a second compilation.
    4. Unikraft Scanner CLI client starts a second compilation and finds all triggered blocks (by checking in stderr of the compilation/make stage for `warning` messages) and sends the compilation coverage and matadata to a centralised database.
    5. Once the static analysis is finished, the Unikraft Scanner scrapes the defects from the Coverity Scan cloud and sends them also to the database.
    6. With the CLI client you can also view a graphical representation of the compilation coverage or delete registered compilations.

- the Coverity Scan cloud environment (in purple):
    1. Contains the closed-source logic for the static analysis.
    2. You can also view found defects by them through a nice GUI interface.
    3. The Unikraft Scanner CLI client scrapes defects from this environment through a semi-automatic process.

- the centralised database:
    1. Used to gather all compiled Unikraft "mutations" and their owning defects.
    2. It is a colaborative environment where you can see progress made by other users, fetch their results and recreate their work locally.

<img src="docs/unikraft_scanner_tool_general.png" alt="drawing" width="1200" height=600/>

A snapshot of how the compilation/scanning is graphically represented can be seen in the image below (green means compiled code, red otherwise). 

![Alt text](docs/result_coverage.jpg) 

# Prerequisites And Configuration 

In order to run this tool you will need: 

1. Install Python 3.8 or above. 

2. Install Python libraries mentioned in `src/requirements.txt`. It is advised to configure a Python virtual environment (venv) that includes these dependencies.

3. Make an account for `https://scan.coverity.com`. **Right now, Github authentication for Coverity is NOT supported when using Unikraft Scanner !**

4. Request `Maintainer` role for the Coverity project `https://scan.coverity.com/projects/unikraft-scanning?tab=overview`. This way, you will be able to submit compilations for analysis and view the found defects.

5. Request from project owner, through Discord, new credentials for interacting with the centralised database which collects information regarding all registered Unikraft compilations.

6. Once the request is accepted, go to `https://scan.coverity.com/download?tab=cxx` and download the `Coverity Build Tools` (linux64). It is recommended to unzip the archive in the `extern` directory where all other external dependecies for the tool (such as a non-snap Firefox or other static analyzers) will reside. The Coverity Build Tools solve the steps 1 and 2 from the Core Concepts diagram.

7. Run `python3 coverage.py setup -s <path where to generate config file>`. This command will start the setup process of the Unikraft Scanner tool.

8. If you wish to change some configuration options, manually edit the generated config file. Do not rerun `setup` command.

# Usage 

Source code is found in `src/`. Tool main usage is through `src/coverage.py`. 

View logging information in the file specified by `logfile` option from the config file 

View tool output in the file specified by `outfile` option from the config file. Output may be too large for console output. 

Adding/registering a new app in order to increase compilation/scan coverage can be done in 2 ways:
The `python3 coverage.py app add` that compiles the Unikraft inage, lets Coverity intercept the build process, sends the Coverity archive to their platform, web-scrapes using Selenium the found defects from said platform and adds enriches the database with these found information. Since the web-scraper is UNSTABLE we will do a semi-automatic quickfix.

````
python3 coverage.py app compile -s <path to yaml config file from step 8>

                             -a <path to a Unikraft app (you can use the ones compatible with catalog)>

                             -t <tag/description (for multi-word put it between double quotes) UNIQUE to both the local DB and Coverity platform>

                             -c <kraft build compilation command>

                             -b <optional path where the Unikraft core and libraries are locally compiled>
````

The seconds method which is the above mentioned quickfix is reprsented by a split of the previously mentioned steps and a manual fetch of found defects from the Coverity platform.


````
python3 coverage.py app compile -s <path to yaml config file from step 8>
                                
                                -a <path to a Unikraft app (you can use the ones compatible with catalog)>

                                -t <tag/description (for multi-word put it between double quotes) UNIQUE to both the database and Coverity platform>

                                -c <kraft build compilation command>

                                -b <optional path where the Unikraft core and libraries are locally compiled>
````

Now you have compiled the Unikraft image and allowed Coverity to also intercept the build.
The result is an archive found in the `-a` directory. In addition, information about the involved source code files from Unikraft is written to the database. However, the information structure is incomplete; we need to 'enrich' it with defects found after Coverity analyzes the archive.

We need to send the archive to the Coverity platform.

````
python3 coverage.py app submit -s <path to yaml config file from step 8>

                               -c <kraft compilation command used in the previous step>

                               -t <UNIQUE tag used in the previous step>

                               -a <path to the archive (usually found the the app directory that has been previously compiled)>
````
Now, the manual part begins, since the web-scraper is not stable and various issues may appepar.

Go to the Coverity Scan project `https://scan.coverity.com/projects/unikraft-scanning?tab=overview`, log in with the the credentials configured during the `setup` phase. Now click `View Defects` button.

From the top-left burger button, go to `SNAPSHOTS` pannel and click on `All in Project`. This is where all app registeration attempts where made with the Unikraft Scanner tool.

Wait for the analysis process to finish (the snapshot whose name is the tag used during `app compile` step will eventually appear in the table).

Double click on the snapshot.

In the top-left corner, click of the Settings wheel symbol, click on the `Column` button and select the additional columns: `Checker`, `CWE`, `Line Number`, `Score`.

Toggle the `Save as Copy` and enter a name for this updated snapshot.

Go back to the top-left burger button, go to `ISSUES: BY SNAPSHOT`, click on the small arrow button coresponding to the newly created snapshot and click `Export CSV`.

Congratulation we have fetched the analysis results. The only step is to also include these found defects in the database

````
python3 coverage app enrich -s <path to yaml config file from step 8>

                            -t <UNIQUE tag used in the previous step>

                            -r <path to where the csv file with the defects has been downloaded to>
````

You can check the log file to see tool progress in real time. After tool execution, go to the Coverity Scan project `https://scan.coverity.com/projects/unikraft-scanning?tab=overview`, click the `View Defects` button and inspect found defects.