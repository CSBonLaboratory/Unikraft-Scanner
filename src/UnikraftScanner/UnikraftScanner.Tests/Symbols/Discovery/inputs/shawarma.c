#include "stdio.h"
#include "string.h"
/*

#ifdef WRONG
int z = 2;
#endif
*/
int main(){

    int x = 7;

    // #if defined(CONFIG_LIB2)
#if defined(CONFIG_LIB1)
    printf("aaaa");
// #else
#elif defined(CONFIG_LIB2)
    printf("vvvv");
//#endif
    #ifndef CONFIG_LIB3


    printf("x");
    #elif defined(CONFIG_LIB4)

        int a;
        #ifdef CONFIG_LIB4 && \
        \
        CONFIG_LIB8
        // aaaa
            printf("aaaa");
            #ifndef CONFIG_LIB5
#include "zzz.h"
            #endif
        #endif

        a++;    /*aaaaaaa #ifdef*/
        printf("%d", a);
        

    #else
        #ifdef CONFIG_LIB6
#include "zzz.h"
        #endif
    printf("xxx");
    #endif
#elif defined(CONFIG_LIB7)
    #ifdef defined(CONFIG_LIB8)
    
    /*aa
    aaaa
        float y=7;
    aaa
    */

    int z = 8;
    #endif
#endif
}