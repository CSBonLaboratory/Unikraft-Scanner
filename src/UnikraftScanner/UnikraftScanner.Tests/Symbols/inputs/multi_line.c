
#if defined(C) \
\
\
&& \
\
defined(D)

void f(){}
void \
\
g(){}

#endif

int main() \
\
{

#ifdef C || \
D \
&& \
A

int x;
#endif


}