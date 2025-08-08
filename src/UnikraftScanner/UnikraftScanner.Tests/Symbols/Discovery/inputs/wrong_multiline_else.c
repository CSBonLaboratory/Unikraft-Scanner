
void f(){
    return;
}
int main(){

#if !defined(B)
    f(); f();

#\
e\
l\
s\
e

f();
#endif

}