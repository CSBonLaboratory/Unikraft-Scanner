#!/bin/bash

cd "$2"
if [ "$1" = "--pipeline" ]; then
    echo "Get Coverity tool archive"
    wget https://scan.coverity.com/download/linux64 --post-data "token=${COVERITY_UPLOAD_TOKEN}&project=${COVERITY_PROJECT_NAME}" -O coverity_tool.tgz
    tar zxvf coverity_tool.tgz
fi


ls -a
cov-build --dir cov-int kraft build --plat qemu --arch x86_64

tar czvf analysis_input.tgz cov-int
curl --form token="$COVERITY_UPLOAD_TOKEN" --form email=csbon420@protonmail.com --form "file=@./analysis_input.tgz" --form version=1 --form description="Submited via Github action" https://scan.coverity.com/builds?project=Unikraft-Scanning
#rm analysis_input.tgz
#rm -r cov-int