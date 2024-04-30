#!/bin/bash

if test "$#" -ne 6; then
    echo "Illegal number of parameters: expected 6 got $#"
    echo "Usage ./upload.sh <app path> <log file> <compile cmd> <upload token> <user email> <description/compilation tag> <project name>"
    exit 1
fi

app_path=$1
compile_cmd=$2
upload_token=$3
user_email=$4
compilation_tag=$5
project_name=$6

cd $app_path
# if [ "$UK_COV_MODE" = "--pipeline" ]; then
#     echo "Get Coverity tool archive"
#     wget https://scan.coverity.com/download/linux64 --post-data "token=${COVERITY_UPLOAD_TOKEN}&project=${COVERITY_PROJECT_NAME}" -O coverity_tool.tgz
#     tar zxvf coverity_tool.tgz
# fi

cov-build --dir cov-int $compile_cmd

tar czvf analysis_input.tgz cov-int
curl --form token="$upload_token" --form email=$user_email --form "file=@./analysis_input.tgz" --form version=1 --form description="$compilation_tag" https://scan.coverity.com/builds?project=$project_name
rm analysis_input.tgz
rm -r cov-int