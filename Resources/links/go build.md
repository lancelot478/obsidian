#Muffin

 go install -ldflags="-linkmode=external" github.com/go-delve/delve/cmd/dlv@v1.22.1


dyld[26938]: missing LC_UUID load command in /Users/gexianglin/go/bin/dlv
dyld[26938]: missing LC_UUID load command

codesign -s - /Users/gexianglin/go/bin/dlv




The issue persists. This is a known issue with Go's linker on macOS, especially with newer versions of macOS that have stricter security requirements. Let me try rebuilding with CGO enabled, which should force Go to use the system linker that includes UUID information:



reCGO_ENABLED=1 go build -v -ldflags "-linkmode external -X server/pkg/build.buildTime=$(date +%Y-%m-%dT%H:%M:%S%z) -X server/pkg/build.buildBranch=$(git rev-parse --abbrev-ref HEAD) -X server/pkg/build.buildHash=$(git log -1 --format='%h') -X server/pkg/build.buildAuthor=$(whoami)" -o bin/zgame server/cmd/zgame

go build -trimpath -v -ldflags "-X server/pkg/build.buildTime=2025-08-01T11:39:30+0800 -X server/pkg/build.buildBranch=develop -X server/pkg/build.buildHash=263edf4359 -X server/pkg/build.buildAuthor=gexianglin -w -s" -o /Users/gexianglin/zserver/bin/zgame server/cmd/zgame