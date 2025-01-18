#ifdef CONFIG_LIB1 && \
CONFIG_LIB2 ||\
    CONFIG_LIB3

void f(){
    printf("aaaa")
    int a = 5 + \
    6;
    if(1 == 1){
        return;
    }
    else
        return;
}
#elif CONFIG_LIB4    \
        \
&& CONFIG_LIB5      
void g(){
    printf("xxxx");
    return;
}
#else
    int main(){
        return -1;
    }
#endif

