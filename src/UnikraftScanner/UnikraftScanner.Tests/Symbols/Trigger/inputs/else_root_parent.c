

int main(){

#if defined(A) // 0


#elif defined(B)  // 1
#elif defined(C)   // 2


#else   // 3


#endif

#if defined(X)  // 4


#elif defined(Y)   // 5
#elif defined(Z)   // 6


#else   // 7


#endif

#if defined T // 8

#elif defined Y // 9

#elif defined U // 10


#endif


#if defined(I)   // 11


#elif defined(J)   // 12
#elif 1   // 13


#else  // 14

#endif




}