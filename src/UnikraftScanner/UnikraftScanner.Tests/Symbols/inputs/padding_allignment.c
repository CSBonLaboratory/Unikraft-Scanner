
#include "stdio.h"

void main(){

    #ifdef CONFIG_LIB1 ||\
            CONFIG_LIB2 &&\
CONFIG_LIB3

printf("aaaaaa");

        printf("bbbb");

#elif CONFIG_LIB2 
                printf("xxxx");

        #endif
}