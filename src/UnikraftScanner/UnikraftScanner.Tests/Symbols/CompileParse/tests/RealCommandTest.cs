namespace UnikraftScanner.Tests;

using Xunit.Abstractions;
using UnikraftScanner.Client.Symbols;
using System.Reflection;

public class RealCommandTest
{
    private readonly ITestOutputHelper output;

    public RealCommandTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    [Trait("Category", "CompileParse")]
    public void DiscoveryStage_RealCompilationCommand()
    {
        string oDotCmdFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "../../../Symbols/CompileParse/inputs/real_command.o.cmd");


        NormalCommandResult expected = new(
            includeTokens: new[]{
                "-I/home/nginx/1.25/.unikraft/unikraft/plat/kvm/includ",
                "-I/home/nginx/1.25/.unikraft/unikraft/plat/common/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/plat/drivers/include",
                "-I/home/nginx/1.25/.unikraft/build/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/arch/x86/x86_64/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/devfs/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/isrlib/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/nolibc/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/nolibc/arch/x86_64",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/nolibc/musl-imported/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/nolibc/musl-imported/arch/generic",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/nolibc/musl-imported/arch/x86_64",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-fdtab/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-fdio/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-eventfd/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-pipe/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-poll/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-process/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-process/arch/x86_64/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-process/musl-imported/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-process/musl-imported/arch/generic",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-socket/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-sysinfo/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-futex/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-timerfd/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-tty/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/posix-user/musl-imported/include",
                "-I/home/nginx/1.25/.unikraft/build/libsyscall_shim/include -I/home/nginx/1.25/.unikraft/unikraft/lib/syscall_shim/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/syscall_shim/arch/x86_64/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/ukalloc/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/ukallocbbuddy/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/ukallocpool/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/ukargparse/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/ukatomic/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/ukbitops/include",
                "-I/home/nginx/1.25/.unikraft/unikraft/lib/ukbitops/arch/x86_64/include",

            },
            orderedSymbolDefineTokens: new[]{
                "-U", "__linux__",
                "-U", "__FreeBSD__",
                "-U", "__sun__",
                "-D", "__Unikraft__",
                "-DUK_CODENAME=\"Calypso\"",
                "-DUK_VERSION=0.17",
                "-DUK_FULLVERSION=0.17.0~e2ccac21",
                "-D__X86_64__",
                "-DCC_VERSION=13.1",
                "-DCONFIG_UK_NETDEV_SCRATCH_SIZE=64",
                "-DKVMPLAT",
                "-DUK_USE_SECTION_SEGMENTS",
                "-D__LIBNAME__=libkvmplat",
                "-D__BASENAME__=io.c"
            },
            otherTokens: new[]{
                "-nostdlib",
                "-nostdinc",
                "-fno-stack-protector",
                "-Wall",
                "-Wextra",
                "-fno-tree-sra",
                "-fno-split-stack",
                "-fcf-protection=none",
                "-O2",
                "-m64",
                "-mno-red-zone",
                "-fno-asynchronous-unwind-tables",
                "-fno-reorder-blocks",
                "-mtune=generic",
                "-fno-builtin-printf",
                "-fno-builtin-fprintf",
                "-fno-builtin-sprintf",
                "-fno-builtin-snprintf",
                "-fno-builtin-vprintf",
                "-fno-builtin-vfprintf",
                "-fno-builtin-vsprintf",
                "-fno-builtin-vsnprintf",
                "-fno-builtin-scanf",
                "-fno-builtin-fscanf",
                "-fno-builtin-sscanf",
                "-fno-builtin-vscanf",
                "-fno-builtin-vfscanf",
                "-fno-builtin-vsscanf",
                "-fno-builtin-exit",
                "-fno-builtin-exit-group",
                "-g3",
                "-c",
                "/home/nginx/1.25/.unikraft/unikraft/plat/kvm/io.c",
                "-o",
                "/home/nginx/1.25/.unikraft/build/libkvmplat/io.o",
                "-Wp,-MD,/home/nginx/1.25/.unikraft/build/libkvmplat/.io.o.d"

            },
            sourceFileAbsPath: "/home/nginx/1.25/.unikraft/unikraft/plat/kvm/io.c",
            compiler: "gcc",
            compilerCategory: "gcc",
            fullCommand: "gcc -nostdlib -U __linux__ -U __FreeBSD__ -U __sun__ -nostdinc -fno-stack-protector -Wall -Wextra -D __Unikraft__ -DUK_CODENAME=\"Calypso\" -DUK_VERSION=0.17 -DUK_FULLVERSION=0.17.0~e2ccac21 -fno-tree-sra -fno-split-stack -fcf-protection=none -O2 -fno-omit-frame-pointer -fdata-sections -ffunction-sections -flto -fno-PIC -fno-tree-loop-distribute-patterns      -I/home/nginx/1.25/.unikraft/unikraft/plat/kvm/include -I/home/nginx/1.25/.unikraft/unikraft/plat/common/include -I/home/nginx/1.25/.unikraft/unikraft/plat/drivers/include -I/home/nginx/1.25/.unikraft/build/include -I/home/nginx/1.25/.unikraft/unikraft/arch/x86/x86_64/include -I/home/nginx/1.25/.unikraft/unikraft/include -I/home/nginx/1.25/.unikraft/unikraft/lib/devfs/include -I/home/nginx/1.25/.unikraft/unikraft/lib/isrlib/include -I/home/nginx/1.25/.unikraft/unikraft/lib/nolibc/include -I/home/nginx/1.25/.unikraft/unikraft/lib/nolibc/arch/x86_64 -I/home/nginx/1.25/.unikraft/unikraft/lib/nolibc/musl-imported/include -I/home/nginx/1.25/.unikraft/unikraft/lib/nolibc/musl-imported/arch/generic -I/home/nginx/1.25/.unikraft/unikraft/lib/nolibc/musl-imported/arch/x86_64 -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-fdtab/include -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-fdio/include -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-eventfd/include -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-pipe/include -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-poll/include -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-process/include -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-process/arch/x86_64/include -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-process/musl-imported/include -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-process/musl-imported/arch/generic -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-socket/include -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-sysinfo/include -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-futex/include -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-timerfd/include -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-tty/include -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-user/musl-imported/include -I/home/nginx/1.25/.unikraft/build/libsyscall_shim/include -I/home/nginx/1.25/.unikraft/unikraft/lib/syscall_shim/include -I/home/nginx/1.25/.unikraft/unikraft/lib/syscall_shim/arch/x86_64/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukalloc/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukallocbbuddy/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukallocpool/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukargparse/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukatomic/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukbitops/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukbitops/arch/x86_64/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukstreambuf/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukblkdev/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukboot/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukbus/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukconsole/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukcpio/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukdebug/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukfalloc/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukfallocbuddy/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukfile/include -I/home/nginx/1.25/.unikraft/unikraft/lib/uklibid/include -I/home/nginx/1.25/.unikraft/build/libuklibid/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukintctlr/include -I/home/nginx/1.25/.unikraft/unikraft/lib/uklibparam/include -I/home/nginx/1.25/.unikraft/unikraft/lib/uklock/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukmpi/include -I/home/nginx/1.25/.unikraft/unikraft/lib/uknetdev/include -I/home/nginx/1.25/.unikraft/unikraft/lib/uksched/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukschedcoop/include -I/home/nginx/1.25/.unikraft/unikraft/lib/uksglist/include -I/home/nginx/1.25/.unikraft/unikraft/lib/uksignal/include -I/home/nginx/1.25/.unikraft/unikraft/lib/uksp/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukstore/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukrandom/include -I/home/nginx/1.25/.unikraft/unikraft/lib/posix-time/include -I/home/nginx/1.25/.unikraft/unikraft/lib/uktimeconv/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukvmem/include -I/home/nginx/1.25/.unikraft/unikraft/lib/vfscore/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukreloc/arch/x86_64/include/uk -I/home/nginx/1.25/.unikraft/unikraft/lib/ukreloc/include -I/home/nginx/1.25/.unikraft/unikraft/lib/ukallocstack/include -I/home/nginx/1.25/.unikraft/libs/libelf/include -I/home/nginx/1.25/.unikraft/libs/lwip/include -I/home/nginx/1.25/.unikraft/libs/lwip/musl-imported/include -I/home/nginx/1.25/.unikraft/build/liblwip/origin/fork-lwip-UNIKRAFT-2_1_x/src/include -I/home/nginx/1.25/.unikraft/unikraft/drivers/ukbus/pci/include -I/home/nginx/1.25/.unikraft/unikraft/drivers/ukbus/platform/include -I/home/nginx/1.25/.unikraft/unikraft/drivers/ukbus/platform/include -I/home/nginx/1.25/.unikraft/unikraft/drivers/ukintctlr/xpic/include -D__X86_64__ -m64 -mno-red-zone -fno-asynchronous-unwind-tables -fno-reorder-blocks -mtune=generic -DCC_VERSION=13.1 -fno-builtin-printf -fno-builtin-fprintf -fno-builtin-sprintf -fno-builtin-snprintf -fno-builtin-vprintf -fno-builtin-vfprintf -fno-builtin-vsprintf -fno-builtin-vsnprintf -fno-builtin-scanf -fno-builtin-fscanf -fno-builtin-sscanf -fno-builtin-vscanf -fno-builtin-vfscanf -fno-builtin-vsscanf -fno-builtin-exit -fno-builtin-exit-group -DCONFIG_UK_NETDEV_SCRATCH_SIZE=64  -DKVMPLAT -DUK_USE_SECTION_SEGMENTS     -g3 -D__LIBNAME__=libkvmplat -D__BASENAME__=io.c -c /home/nginx/1.25/.unikraft/unikraft/plat/kvm/io.c -o /home/nginx/1.25/.unikraft/build/libkvmplat/io.o -Wp,-MD,/home/nginx/1.25/.unikraft/build/libkvmplat/.io.o.d"
        );

        Assert.Equal(expected, new NormalCompilationCommandParser().ParseCommand(oDotCmdFilePath));
    }

}
