/*
 * Contents/MacOS/TheUltra: starts the .NET runtime in Contents/MonoBundle and runs the game
 * in this process, so the app keeps this executable's signature and sandbox entitlements.
 * Built by tools/build_desktop.sh.
 */
#include <dlfcn.h>
#include <libgen.h>
#include <limits.h>
#include <mach-o/dyld.h>
#include <stdio.h>
#include <stdlib.h>

/* AppKit: connects to the window server and registers with LaunchServices. */
extern int NSApplicationLoad(void);

typedef int (*hostfxr_main_startupinfo_fn)(int argc, const char **argv, const char *host_path,
                                           const char *dotnet_root, const char *app_path);

int main(int argc, const char **argv)
{
    char exe[PATH_MAX], real[PATH_MAX], path[PATH_MAX], root[PATH_MAX];
    char hostfxr[PATH_MAX], host[PATH_MAX], app[PATH_MAX];
    uint32_t size = sizeof exe;

    if (_NSGetExecutablePath(exe, &size) != 0 || realpath(exe, real) == NULL)
        return 1;
    snprintf(path, sizeof path, "%s/../MonoBundle", dirname(real));
    if (realpath(path, root) == NULL)
    {
        fprintf(stderr, "The Ultra: %s not found\n", path);
        return 1;
    }
    snprintf(hostfxr, sizeof hostfxr, "%s/libhostfxr.dylib", root);
    snprintf(host, sizeof host, "%s/TheUltra", root);
    snprintf(app, sizeof app, "%s/TheUltra.dll", root);

    /* Register the app before the runtime starts: in the sandbox, LaunchServices refuses a
       process that first connects after the .NET runtime has set itself up. */
    NSApplicationLoad();

    /* No debugger or diagnostics IPC: it isn't needed and the sandbox doesn't allow it. */
    setenv("DOTNET_EnableDiagnostics", "0", 1);

    void *lib = dlopen(hostfxr, RTLD_NOW | RTLD_LOCAL);
    if (lib == NULL)
    {
        fprintf(stderr, "The Ultra: %s\n", dlerror());
        return 1;
    }
    hostfxr_main_startupinfo_fn run = (hostfxr_main_startupinfo_fn)dlsym(lib, "hostfxr_main_startupinfo");
    if (run == NULL)
    {
        fprintf(stderr, "The Ultra: %s\n", dlerror());
        return 1;
    }
    return run(argc, argv, host, root, app);
}
