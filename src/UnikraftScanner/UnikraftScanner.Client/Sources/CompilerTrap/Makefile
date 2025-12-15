
# passed from PrepCompilableSourcesEnvFixture and used in this Makefile
CXX ?=
COMPILER_TRAP_BINARY ?=

# this is passed from PrepCompilableSourcesEnvFixture and then passed to ClangTrap.cxx as predefined symbols
HOST_COMPILER_ARG ?=
RESULTS_FILE_PATH_ARG ?=

CXXFLAGS = -Wall -Wextra -D HOST_COMPILER=\"$(HOST_COMPILER_ARG)\" -D RESULTS_FILE_PATH=\"$(RESULTS_FILE_PATH_ARG)\"
SRC = ClangTrap.cxx
OBJ = bin/ClangTrap.o

all: $(COMPILER_TRAP_BINARY)


$(COMPILER_TRAP_BINARY): $(OBJ)
	$(CXX) -o $(COMPILER_TRAP_BINARY) $(OBJ)
	rm -f $(OBJ)

$(OBJ): $(SRC)
	$(CXX) $(CXXFLAGS) -c $< -o $@

clean:
	rm -f $(COMPILER_TRAP_BINARY)