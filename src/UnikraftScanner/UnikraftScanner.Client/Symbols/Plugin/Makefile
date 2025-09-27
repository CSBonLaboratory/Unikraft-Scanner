CXX = clang++-18
CXXFLAGS = -Wall -Wextra -fPIC -std=c++17 -fno-exceptions -funwind-tables -I/usr/lib/llvm-18/include -c 
LDFLAGS = -shared

# Source files and object files
SRC = BlockInterceptorPlugin.cxx
OBJ = ./bin/BlockFinder.o
OUTPUT = ./bin/BlockFinderPlugin.so

# Default target: build the shared object
all: so

# Rule to create the shared object (.so)
so: object
	$(CXX) $(OBJ) $(LDFLAGS) -o $(OUTPUT)

# Rule to build object files
object: 
	$(CXX) $(CXXFLAGS) -c $(SRC) -o $(OBJ)

# Clean up build files
clean:
	rm -f $(OBJ) $(OUTPUT)