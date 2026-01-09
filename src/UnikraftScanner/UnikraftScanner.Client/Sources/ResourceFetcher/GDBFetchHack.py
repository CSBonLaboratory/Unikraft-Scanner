# python script for the GDB that will put a breakpoint in the kraftkit binary (with DEBUG symbols) immediately after fetching resources
# and exiting that breakpoint in order not to compile the Unikraft application
# compilation of the Unikraft application will be done later when we find all source files and apply the compiler plugin to find compilation blocks for each
# source file found 

import gdb

# gdb will usually go after a child but the main logic which includes the resource fetching of a Unikraft app is done in the parent process
# so instruct the debugger to remain in the parent
gdb.execute("set follow-fork-mode parent")

breakpoint_name = "kraftkit.sh/unikraft/app.(*application).Build"


# this is used to check that the breakpoint was hit in the BinCompatHelper.cs
success_msg = "KRAFT HACK SUCCESS"

class BreakpointAfterFetch (gdb.Breakpoint):
      def __init__ (self):
        super(BreakpointAfterFetch, self).__init__ (breakpoint_name)
      def stop (self):

        print(f"Unikraft Scanner reached endpoint {self.location}")
        print(success_msg)

        # lawful exit which will be checked in GDBKraftkitFetcher.cs
        gdb.execute("exit 0")
        return False

x = BreakpointAfterFetch()

# if inferior (the debuged kraft process) exits with an error it will be checked here
# and if is any error, kill the gdb also with same error as in kraft
# now the GDBKraftkitFetcher.cs will not wait indefinetelly if there is any error in gdb (look WaitForExit() method)
# without this emergency exit, the kraft would have been kill but we would be stuck in a prompt inside gdb
def kraft_end_check_handler(event : gdb.ExitedEvent):

  # it seems that if the kraft fnishes OK and GDB has exit code 0, the event will not have exit_code attribute (maybe bug in the GDB)
  try:
    if event.exit_code != 0:
      
      # bad exit which will be checked in GDBKraftkitFetcher.cs
      gdb.execute(f"exit {event.exit_code}")
  except:
    print("GDB successful or an unthinkable error")
  

gdb.events.exited.connect(kraft_end_check_handler)

