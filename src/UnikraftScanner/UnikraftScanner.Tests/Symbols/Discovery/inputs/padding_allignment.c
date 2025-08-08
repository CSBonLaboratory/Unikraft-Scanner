
#include <stdio.h>

int main(){
        #if defined(A)
        #endif
    #ifdef      CONFIG_LIB1

printf("aaaaaa");

        printf("bbbb");

#elif                   defined(CONFIG_LIB2) 
                printf("xxxx");

        #else        
                printf("aaaaa");

                  #endif
}