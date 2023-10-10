#!/bin/bash

REALPATH=$(realpath ${BASH_SOURCE[0]})
SCRIPT_DIR=$(cd $(dirname ${REALPATH}) && pwd)
WORK_DIR=$(cd $(dirname ${REALPATH})/.. && pwd)
echo "BASH_SOURCE=${BASH_SOURCE}, REALPATH=${REALPATH}, SCRIPT_DIR=${SCRIPT_DIR}, WORK_DIR=${WORK_DIR}"
cd ${WORK_DIR}

help=no
refresh=no

while [[ "$#" -gt 0 ]]; do
    case $1 in
        -h|--help) help=yes; shift ;;
        -refresh|--refresh) refresh=yes; shift ;;
        *) echo "Unknown parameter passed: $1"; exit 1 ;;
    esac
done

if [[ "$help" == yes ]]; then
    echo "Usage: $0 [OPTIONS]"
    echo "Options:"
    echo "  -h, --help           Show this help message and exit"
    echo "  -refresh, --refresh  Refresh current tag. Default: no"
    exit 0
fi

RELEASE=$(git describe --tags --abbrev=0 --match v*)
if [[ $? -ne 0 ]]; then echo "Release failed"; exit 1; fi

REVISION=$(echo $RELEASE|awk -F . '{print $3}')
if [[ $? -ne 0 ]]; then echo "Release failed"; exit 1; fi

let NEXT=$REVISION+1
if [[ $refresh == yes ]]; then
  let NEXT=$REVISION
fi
echo "Last release is $RELEASE, revision is $REVISION, next is $NEXT"

MINOR=$(echo $RELEASE |awk -F '.' '{print $2}')
VERSION="1.$MINOR.$NEXT" &&
TAG="v$VERSION" &&
BRANCH=$(git branch |grep '*' |awk '{print $2}') &&
echo "publish version $VERSION as tag $TAG, BRANCH=${BRANCH}"
if [[ $? -ne 0 ]]; then echo "Release failed"; exit 1; fi


# Update code for Unity Editors.
for dir in $(ls -d unity-editor-*); do
  echo "Update code for $dir"
  (
    cd $dir/Assets &&
    rm -rf io.ossrs io.ossrs.meta &&
    cp -r ../../src/Assets/io.ossrs* . &&
    git add .
  )
  if [[ $? -ne 0 ]]; then echo "Update code for Unity Editors failed"; exit 1; fi
done

# Make sure all changes are committed.
git st |grep -q 'nothing to commit'
if [[ $? -ne 0 ]]; then
  echo "Failed: Please commit before release";
  exit 1
fi

git fetch origin
if [[ $(git status |grep -q 'Your branch is up to date' || echo 'no') == no ]]; then
  git status
  echo "Failed: Please sync before release";
  exit 1
fi
echo "Sync OK"

git tag -d $TAG 2>/dev/null && git push origin :$TAG
git tag $TAG
git push origin $TAG
echo "publish $TAG ok"
echo "    https://github.com/ossrs/srs-unity/actions"
