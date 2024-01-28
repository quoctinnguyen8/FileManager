#include <stdio.h>

int main() {
    int gio, phut, giay, thems;

    // Nhap gio, phut, giay
    printf("Nhap gio: ");
    scanf("%d", &gio);

    printf("Nhap phut: ");
    scanf("%d", &phut);

    printf("Nhap giay: ");
    scanf("%d", &giay);

    // Nhap so giay can them
    printf("Nhap so giay can them: ");
    scanf("%d", &thems);

    // Cong giay
    giay += thems;

    
    if (giay >= 60) 
	{
        giay -= 60;
        phut++;
    }

    if (phut >= 60) 
	{
        phut -= 60;
        gio++;
    }

    // In kq
    printf("Ket qua: %d:%d:%d\n", gio, phut, giay);

    return 0;
}

