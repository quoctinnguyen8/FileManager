#include <stdio.h>

int main() {
	//câu 17:
//	int n;
//	printf("vui long nhap gia tri do dai cua n: "); scanf("%d",&n);
//   int arr[n];
//   int i, j, temp;
//
//   printf("Nhap 3 so a b c: ");
//   for (i = 0; i < n; i++){
//   	    scanf("%d", &arr[i]);
//   }
//    
//
//   for (i = 0; i < n; i++) {
//       for (j = 0; j < n-i; j++) {
//           if (arr[j] > arr[j+1]) {
//               temp = arr[j];
//               arr[j] = arr[j+1];
//               arr[j+1] = temp;
//           }
//       }
//   }
//
//   printf("So sau khi sap xep: ");
//   for (i = 0; i < n; i++)
//       printf("%d ", arr[i]);
//   printf("\n");
//   
   
   //câu 18:
    int hours, minutes, seconds, N;
    printf("Nhap gio: ");
    scanf("%d", &hours);
    printf("Nhap phut: ");
    scanf("%d", &minutes);
    printf("Nhap giay: ");
    scanf("%d", &seconds);
    printf("Nhap N giay: ");
    scanf("%d", &N);
	
    int total_seconds = hours * 3600 + minutes * 60 + seconds + N; //N cong them
    hours = total_seconds / 3600;
    total_seconds %= 3600;	
    minutes = total_seconds / 60;
    seconds = total_seconds % 60;
    if(hours >=24){
    	hours = hours / 24;
	}
	    printf("Ket qua: %02d:%02d:%02d\n", hours, minutes, seconds);


	
	return 0;
}
