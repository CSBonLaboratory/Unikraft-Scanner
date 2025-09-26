
void f() {}
int main(){

#ifdef A // 0

    #ifndef D // 1


        #if defined(E) &&\
        \
        \
        \
        defined(F) // 2

            f();

            #ifndef G // 3

                #if defined(H) // 4
                    f();
                #endif
                #if !defined(H) // 5
                    f();
                    f();
                #elif 0 // 6
                    f();
                #else // 7
                    f();
                #endif
                #if defined(I) // 8
                    f();
                    f();
                #else // 9
                    #ifdef I // 10
                        f();
                    #else // 11
                        f();
                    #endif
            
                #endif

                #ifndef \
                \
                J // 12
                #elif \
                \
                \
                K // 13
                #else // 14
                #endif

            #endif
        #elif !defin\
        ed(F)  // 15



        #elif !\
        \
        \
        defined(E)  // 16
        #endif
    #endif

    #if 0  // 17

    #elif 0 // 18


    #elif 0  // 19

    #else // 20
    #endif

#elif B // 21

    #ifdef L // 22

    #endif

    float g=7.8;
    g++;
    
    #if defined M  // 23

        f();
    #else // 24
   
        #ifndef P // 25

        #elif defined(P) // 26
            #ifdef R  // 27
                f();
            #endif

            #ifndef S // 28
                f();
            #endif
        #else // 29
            #if !defined(R) // 30

                f();
            #else // 31
                int q = 7;
            #endif
            
        #endif

        #if 0 // 32
            f();
        #else //33
        #endif
        f();
        f\
        ();


    #endif

#elif C // 25  34


#else // 26  35


#endif

#if defined(N) // 27  36

    #if 0  // 28  37

    #elif 1  // 29  38

    #else  // 30  39

    #endif
#endif

}

#ifndef O  // 31  40
    #if 1 // 32  41
        void x();
    #elif 0  // 33  42
        void x();
    #else // 34   43
        void x();
    #endif
#endif


#if defined(U)  // 44
    void x();
#elif defined(V)  //45
    void x();

#endif