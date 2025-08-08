
void f(){
    return;
}
int main(){

#if !defined(A)
#     ifdef B
        f();

        f();
#endif

#if\
ndef V
    f(); f(); f(); f(); f(); f(); 
    #endif
#endif

}