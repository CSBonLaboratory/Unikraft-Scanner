int main(){


#ifdef A
#elif B
    #ifndef E
        #if defined(G)
        #elif defined(H)
        #else
            #ifndef I
            #elif defined(J)
            #endif
        #endif
    #endif
    #ifdef A
    #endif

    #ifndef B
    #endif
#elif C

#elif D

    #if defined(K)
    #endif

    #ifdef L
    #endif
#endif
}