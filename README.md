# Unikraft Scanner

Unikraft Scanner is a tool intended for developers or CI/CD pipelines, which solves issues regarding the scanning coverage of third-party static code analysis security tools on the Unikraft repo. 
The most powerful static code analysis checkers such as Coverity or CodeQL somehow intercept the compilation process of the targeted codebase. This means that in order for the Unikraft code repository to be fully analyzed, we need to trigger multiple compilations of all core modules (which is difficult) that sum the totality of the codebase. In addition, compiling a core module does not guarantee that all code lines have been compiled (there are multiple #if, #ifdef, #ifndef that can remove parts of code since their symbol conditions are not satisfied). 

The issue of compilation coverage and not having a way to visualize what code regions of the Unikraft codebase are analyzed, is shown below.

![Alt text](docs/compile_cov.png)

# Core Concepts



# Prerequisites And Configuration

In order to run this tool you will need:
1. Python 3.8 or above.
2. Python libraries mentioned in `src/requirements.txt`. It is advised to configure a Python virtual environemnt (venv) that includes this dependencies.
3. Run a local MongoDB database instance. The Dockefile placed in the root directory can be used for this step.
4. A valid account for `https://scan.coverity.com`. You can create it using your Github account.
5. Request `Maintainer/owner` role for the Coverity project #TODO
6. Once request is accepted, go to `https://scan.coverity.com/projects/unikraft-scanning/builds/new` and get the Coverity upload API token (should be after the -token=... parameter in the curl commands)

![Alt text](docs/coverity_token.jpg)

7. Create your own configuration file based on the examples found in `src/tool_configs`. File `config_<x>.yaml` represents the version X of the configuration schema. It is advised to use the latest version schema when using the tool.


