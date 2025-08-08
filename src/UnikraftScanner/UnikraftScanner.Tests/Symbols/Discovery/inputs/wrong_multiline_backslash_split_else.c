void f(){
    return;
}
int main(){

#ifdef B
    f(); f();

#\
\
\
else
f(); f(); f()\
\
\
;\
f();
#endif

}