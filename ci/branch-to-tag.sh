TRAVIS_BRANCH=${TRAVIS_TAG:-$TRAVIS_BRANCH}
PARTS=(${TRAVIS_BRANCH//\// })
TAG=:dev
if [ "${PARTS[0]}" == "rel" ]; then
    TAG=:${PARTS[1]}
fi;
if [ "${PARTS[0]}" == "line" ]; then
    TAG=:${PARTS[1]}
fi
if [ "${PARTS[0]}" == "rc" ]; then
    MORE_PARTS=(${PARTS[1]//-/ })
    REST=("${MORE_PARTS[@]:0:1}")
    REST+=("rc")
    if [ "${MORE_PARTS[1]}" != "" ]; then
        REST+=("${MORE_PARTS[@]:1}")
    fi
	echo $REST
    TAG=$(printf -- ".%s" "${REST[@]:1}")
    TAG=:${REST}-${TAG:1}
fi
if [ "${PARTS[0]}" == "latest" ]; then
    TAG=:latest
fi
echo $TAG
