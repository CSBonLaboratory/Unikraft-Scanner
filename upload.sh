#!/bin/bash

cd $UK_COV_APP_PATH
if [ "$UK_COV_MODE" = "--pipeline" ]; then
    echo "Get Coverity tool archive"
    wget https://scan.coverity.com/download/linux64 --post-data "token=${COVERITY_UPLOAD_TOKEN}&project=${COVERITY_PROJECT_NAME}" -O coverity_tool.tgz
    tar zxvf coverity_tool.tgz
fi

cov-build --dir cov-int $UK_COV_APP_COMPILE_CMD

tar czvf analysis_input.tgz cov-int
curl --form token="$COVERITY_UPLOAD_TOKEN" --form email=csbon420@protonmail.com --form "file=@./analysis_input.tgz" --form version=1 --form description="Submited via Github action" https://scan.coverity.com/builds?project=Unikraft-Scanning
rm analysis_input.tgz
rm -r cov-int