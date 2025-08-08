
void f(){
    return;
}
int main(){

#if !defined(A)
#     ifdef B
        f();

        f();
#endif

#i\
f defined(V)
    f(); f(); f(); f(); f(); f(); 
    #endif
#endif

}