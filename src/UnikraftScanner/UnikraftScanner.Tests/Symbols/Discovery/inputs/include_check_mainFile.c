#include "other.h"

int main(){

    int x;
#ifndef A

    x = 7;

#elif !defined(B)
    x = 9;

#elif defined(B)
x=10;

#endif

}
