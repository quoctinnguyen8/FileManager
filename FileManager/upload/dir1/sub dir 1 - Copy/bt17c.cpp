#include <iostream>
#include <math.h>

using namespace std;
int main()
{
	
	//Nhap gia tri a,b,c
	float a,b,c,tang;
	cout<<"nhap a="<<"\n";
	cin>>a;
	cout<<"nhap b="<<"\n";
	cin>>b;
	cout<<"nhap c="<<"\n";
	cin>>c;
	//Neu a > b
	if(a>b)
	{
		tang=a;
		a=b;
		b=tang;
	}
	//Neu a > c
	if(a>c)
	{
		tang=a;
		a=c;
		c=tang;
	}
	//Neu b > c
	if(b>c)
	{
		tang=b;
		b=c;
		c=tang;
	}
	//So thu tu tang dan:
	cout<<"So thu tu tang dan:  "<<a<<" "<<b<<" "<<c<<"\n";
}
