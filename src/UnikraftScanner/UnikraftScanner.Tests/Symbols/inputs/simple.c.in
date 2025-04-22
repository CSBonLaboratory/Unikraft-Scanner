#define AAAAAAA "aaa"
#undef AAAAAAA
int main(){

    int x;

#ifdef CONFIG_1
    int a;
    x = a + 5;
#endif

#ifndef CONFIG_1
    printf("aaaaaa");
    

    printf("vvvv");

#endif


#if defined(CONFIG_1) && !defined(CONFIG_2)
    
    printf("zzzzzzz");

    if(1 == 1){
        printf("aaaa");

        printf("xxx");
    }
    else{
        x = 69;
    }
    x=7;
    printf("xxxxxx");
#endif

    if(x==7){
        printf("qq");
        return -1;
    }
    printf("aaaaaa");
    return 0;
}