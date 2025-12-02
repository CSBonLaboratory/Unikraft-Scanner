# this is passed from PrepSymbolTestEnvFixture as the compiler used to build the plugin itself (used only in this Makefile)
CXX ?= 

# this is passed from PrepSymbolTestEnvFixture as the output to where the freshly compiled plugin will be generated (used only in this Makefile)
PLUGIN_BINARY ?= 

# these are passed from PrepSymbolTestEnvFixture and if not get the default value
# the name is used in the command that loads the plugin and compiles a source file in order to find its compilation blocks and symbols
# it is not necessary to be the same name as the plugin binary name but must be different than other plugins loaded in the same command
REGISTRY_NAME_ARG ?= "ConditionalBlockFinder"

# description is pretty useless
REGISTRY_DESCRIPTION_ARG ?= "Plugin used in Symbol component"

CXXFLAGS = -Wall -Wextra -fPIC -std=c++17 -fno-exceptions -funwind-tables -I/usr/lib/llvm-18/include -c 
LDFLAGS = -shared

# Source files and object files
SRC = BlockInterceptorPlugin.cxx
OBJ = ./bin/BlockFinder.o


all: so

so: object
	$(CXX) $(OBJ) $(LDFLAGS) -o $(PLUGIN_BINARY)

object: 
	$(CXX) -D REGISTRY_NAME=\"$(REGISTRY_NAME_ARG)\" -D REGISTRY_DESCRIPTION=\"$(REGISTRY_DESCRIPTION_ARG)\" $(CXXFLAGS) -c $(SRC) -o $(OBJ)

clean:
	rm -f $(OBJ) $(PLUGIN_BINARY)