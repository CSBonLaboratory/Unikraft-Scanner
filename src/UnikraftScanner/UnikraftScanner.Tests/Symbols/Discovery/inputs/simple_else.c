#include <stdio.h>
int main(){
    int x = 6;

#ifdef CONFIG_1
    printf("aaaa");
    int a = 7;
    int q = 7 + \
    5;
    if(x == 42){
        printf("ZZZZ");
#elif CONFIG_1 || CONFIG_2
    int a = 69; }
    else{
        int q = 42;
    }
#elif !CONFIG_2
    printf("qqfgw");
#else
    printf("xxxx");
    printf("qqqqqqqq");

    int c = 7;

#endif
float graram = 7.65;
#ifdef CONFIG_KVMPLAT
    printf("aaa");
#elif !CONFIG_RUST
    printf("xxxx");
#else
    printf("xxx");
#endif

#if defined(CONFIG_UK)
    x++;
    x++;
#elif !defined(CONFIG_UK) || defined(CONFIG_RUST)
    printf("xxx");
#else


    printf("xxx");
#endif
x++;
#if !defined(CONFIG_LIB)

x = 5;
    printf("xxxxx");
x=7;
#else
x=4;
#endif

#ifdef CONFIG_LIB
    printf("aaaa");

    
#endif

}