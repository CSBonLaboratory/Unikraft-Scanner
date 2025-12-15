#include <string>
#include <filesystem>
#include <fstream>

// Usage: <trap binary name> <path to results file> <arguments populated by the Unikraft Makefile and used in the compilation of a Unikraft source file>
int main(int argc, char* argv[]){


    // results file path passed from PrepCompilableSourcesEnvFixture then Makefile and used here
    std::ofstream resultsOutput(RESULTS_FILE_PATH, std::ios::out | std::ios::app);

    std::string proxyCompileCmd;

    std::string sourceFilePath;

    // instead of the trap compiler called by the Unikraft application's Makefile, we replace with the actual compiler which will
    // do the normal work
    // the define is passed from the Makefile which is passed from PrepCompilableSourcesEnvFixture 
    proxyCompileCmd += HOST_COMPILER;

    proxyCompileCmd += " ";

    bool sourceFound = false;

    for(int i = 1; i < argc; i++){

        std::string cmd_token(argv[i]);
        
        // used to fix https://github.com/CSBonLaboratory/Unikraft-Scanner/issues/16
        // the whole token is `-D__LIBUKLIBID_COMPILER__=Clang <version>` as it was alrady split by the argv mechanism
        // a sneaky mistake would have been to consider the argument only `-D__LIBUKLIBID_COMPILER__=Clang` with no version substring
        // since we have a space but that space is not considered in the tokenization split 
        // because the original command passed to the command line as argument had double quotes "-D__LIBUKLIBID_COMPILER__=Clang <version>"
        if(cmd_token.find("-D__LIBUKLIBID_COMPILER__=") == 0){
            proxyCompileCmd += "-D__LIBUKLIBID_COMPILER__=";
            proxyCompileCmd += "\"";
            proxyCompileCmd += cmd_token.substr(std::string("-D__LIBUKLIBID_COMPILER__=").length());

            proxyCompileCmd += " ";
            proxyCompileCmd += "\" ";

            continue;
        }
        
        if(cmd_token.length() > 2){

            std::string file_extension = cmd_token.substr(cmd_token.length() - 2, 2);

            if(file_extension.compare(".c") == 0 && std::filesystem::exists(cmd_token)){
                sourceFilePath = cmd_token;
                sourceFound = true;
            }
        }

        proxyCompileCmd += argv[i];
        proxyCompileCmd += " ";
        
    }

    if(sourceFound)
        resultsOutput << sourceFilePath << "\n";
    else
        resultsOutput << "None" << "\n";
    resultsOutput<< proxyCompileCmd << "\n";
    resultsOutput<<"\n";

    resultsOutput.close();
    return system(proxyCompileCmd.c_str());

}