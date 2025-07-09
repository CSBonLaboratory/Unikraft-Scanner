#define ZZZ(x){\
\
    int q;\
    char a;\
    char b;\
}

int main(){
    float q,n;
#ifdef A
        q = 6.9;
#else
        q = 4.2;
#endif
    int b;
    ZZZ(b);

#ifndef B
    n = 2.7182;

#elif !defined(C)
    n = 3.14159265;
#else

    n = 4.00;
#endif

    return 0;



}