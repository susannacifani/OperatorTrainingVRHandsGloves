/**
####################################
# Data Glove Communication Package
#    Written by K.Kanev, 2020.
####################################
*/

//define _WIN32
//exit the program by pressing Ctrl-C

#include <stdlib.h>
#include <conio.h>
#include <stdio.h>
//#include <string.h>

//#ifdef _WIN32
//#include <Windows.h>
//#else
//#include <unistd.h>0
//#endif

//#include "rs232.h"

#include <windows.h>

#define RS232_PORTNR  32

HANDLE Cport[RS232_PORTNR];
char mode_str[128];

int RS232_OpenComport(int comport_number, int baudrate, const char *mode, int flowctrl) {
    char tmp_str[100];
    if((comport_number>=RS232_PORTNR)||(comport_number<0))  {
        printf("illegal comport number\n");
        return(1);
    }
    sprintf(mode_str,"baud=%d",baudrate); //baud rate=460800
    sprintf(tmp_str," data=%c",mode[0]); //data bits=8
    strcat(mode_str,tmp_str);
    sprintf(tmp_str," parity=%c",mode[1]);//parity=n
    strcat(mode_str,tmp_str);
    sprintf(tmp_str," stop=%c",mode[2]);//stop bits=1
    strcat(mode_str,tmp_str);
    if(flowctrl)  {
        strcat(mode_str, " xon=off to=off odsr=off dtr=on rts=off");
    }  else  {
        strcat(mode_str, " xon=off to=off odsr=off dtr=on rts=on");
    }
// http://msdn.microsoft.com/en-us/library/windows/desktop/aa363145%28v=vs.85%29.aspx
// http://technet.microsoft.com/en-us/library/cc732236.aspx
// https://docs.microsoft.com/en-us/windows/desktop/api/winbase/ns-winbase-_dcb

//    printf("mode_str=%s\n",mode_str);
//    printf("comport_number=%d\n",comport_number);
    sprintf(tmp_str,"\\\\.\\COM%d",comport_number);//stop bits=1
//    printf("tmp_str=%s\n",tmp_str);
    Cport[comport_number] = CreateFileA(tmp_str,
                      GENERIC_READ|GENERIC_WRITE,
                      0,                          // no share
                      NULL,                       // no security
                      OPEN_EXISTING,
                      0,                          // no threads
                      NULL);                      // no templates
    if(Cport[comport_number]==INVALID_HANDLE_VALUE)  {
        printf("unable to open comport\n");
        return(1);
    }
    DCB port_settings;
    memset(&port_settings, 0, sizeof(port_settings));  /* clear the new struct  */
    port_settings.DCBlength = sizeof(port_settings);
    if(!BuildCommDCBA(mode_str, &port_settings))  {
        printf("unable to set comport dcb settings\n");
        CloseHandle(Cport[comport_number]);
        return(1);
    }
    if(flowctrl)  {
        port_settings.fOutxCtsFlow = TRUE;
        port_settings.fRtsControl = RTS_CONTROL_HANDSHAKE;
    }
    if(!SetCommState(Cport[comport_number], &port_settings))  {
        printf("unable to set comport cfg settings\n");
        CloseHandle(Cport[comport_number]);
        return(1);
    }
    COMMTIMEOUTS Cptimeouts;
    Cptimeouts.ReadIntervalTimeout         = MAXDWORD;
    Cptimeouts.ReadTotalTimeoutMultiplier  = 0;
    Cptimeouts.ReadTotalTimeoutConstant    = 0;
    Cptimeouts.WriteTotalTimeoutMultiplier = 0;
    Cptimeouts.WriteTotalTimeoutConstant   = 0;
    if(!SetCommTimeouts(Cport[comport_number], &Cptimeouts))  {
        printf("unable to set comport time-out settings\n");
        CloseHandle(Cport[comport_number]);
        return(1);
    }
    return(0);
}

int RS232_PollComport(int comport_number, unsigned char *buf, int size) {
    int n;
    ReadFile(Cport[comport_number], buf, size, (LPDWORD)((void *)&n), NULL);
    return(n);
}

void RS232_CloseComport(int comport_number) {
    CloseHandle(Cport[comport_number]);
}

/*
http://msdn.microsoft.com/en-us/library/windows/desktop/aa363258%28v=vs.85%29.aspx
*/

int RS232_SendByte(int comport_number, unsigned char byte) {
    int n;
    WriteFile(Cport[comport_number], &byte, 1, (LPDWORD)((void *)&n), NULL);
    if(n<0)  return(1);
    return(0);
}

void RS232_cputs(int comport_number, const char *text) { /* sends a string to serial port */
    while(*text != 0)   RS232_SendByte(comport_number, *(text++));
}

void o(int n);
void c();
void g(int i);
void b();
void e();
void rd();
void in();
int n;

//mode_str=baud=460800 data=8 parity=n stop=1 xon=off to=off odsr=off dtr=on rts=onOpen COM5
int main() {
    g(4);
    o(5);
    //g(4);
    b();
//    e();
//    g (0);
//    Sleep(10);
//    rd ();
//    Sleep(10);
//    b();
//    Sleep(20);
//    e();
//return(0);
   while(1) {
     in();
     rd ();
     Sleep(1);
   }
//    Sleep(3000);
    c();

  return(0);
}

    char ins[100];
    int ini=0;
void in() {
    char cmd;
    int p1, n;
    if(_kbhit()) {
        ins[ini]=_getch(); ini++;
        if(ins[ini-1]=='\x0d') {
            ins[ini-1]='\0';
//            printf("ins=%s\n",ins);
            p1=0;
            n=sscanf(ins,"%c %d",&cmd,&p1);
//            printf("n=%d\n",n);
//            printf("cmd=%c p1=%d\n",cmd,p1);
            if(n<2){
                printf("%c\n",cmd);
            } else {
                printf("%c %d\n",cmd,p1);
            }
            if(cmd=='o'){
                o(p1);
            }else if(cmd=='c'){
                c();
            }else if(cmd=='b'){
                b();
            }else if(cmd=='e'){
                e();
            }else if(cmd=='g'){
                g(p1);
            }
            ini=0;
        }
    }
}

int cport_nr; // /dev/ttyS0 (COM1 on windows)
void o (int n) { //(o)pen:open serial port comn
//    int bdrate=460800;  //9600 baud
//    char mode[]={'8','N','1',0};
    cport_nr = n; // /dev/ttyS0 is COM1 on windows
    if(RS232_OpenComport(n, 460800, "8n1", 0)) {
        printf("Cannot open COM%d\n", n);
    } else {
        printf("Open COM%d\n", n);
    }
}

void c () { //(c)lose:close the currently open serial port comn
    RS232_cputs(cport_nr, "\xa5\x5a\x11\0"); //request end of data sending
    RS232_CloseComport(cport_nr);
    printf("Close COM%d\n", cport_nr);
}

void g (int s) { //(g)yro:g|g 2|g 4|g 8|g 16
	if (s==0) { //g:get gyro scale value
		RS232_cputs(cport_nr, "\xa5\x5a\x0a\0");
	} else if (s==2) { //g 2:set gyro scale value to 2
		RS232_cputs(cport_nr, "\xa5\x5a\x09\x02\0");
	} else if (s==4) { //g 4:set gyro scale value to 4
		RS232_cputs(cport_nr, "\xa5\x5a\x09\x04\0");
	} else if (s==8) { //g 8:set gyro scale value to 8
		RS232_cputs(cport_nr, "\xa5\x5a\x09\x08\0");
	} else if (s==16) { //g 16:set gyro scale value to 16
		RS232_cputs(cport_nr, "\xa5\x5a\x09\x10\0");
	}
}

int iii;
void b () { //(b)egin:request to begin sending data
	RS232_cputs(cport_nr, "\xa5\x5a\x10");
	iii = 0;
}

void e () { //(e)nd:request to end sending data
	RS232_cputs(cport_nr, "\xa5\x5a\x11");
}

int sm(char s, int rem);
unsigned char buf[5000];

void rd () { //rd: read
    int n, si, rem;
    n = RS232_PollComport(cport_nr, buf, 4000);
//printf("rd n=%d\n",n);
//printf("rd n=%d buf[0]=%02x\n",n,((int)buf[0])&0xff);
	if (n>0) {
		for (si=0; si<n; si++) {
			rem = n - si;
			if (sm(buf[si],rem)==1) { //packet ready
				if (rem>2000) {
					iii++;
					printf("skip 200 iii=%d",iii);
					si = si+200;
				}
			}
		}
	}
}
char h[10000]; //the hex string

void s2x (char *s, int sl); //string to hexadecimal string
void s2d (char *s, int sl); //string to hexadecimal string
int st=0; //initial state 0
char pk[10000];
int i=0;
int sm (char ch, int rem) { //state machine for extracting packet from the input chain
//printf("sm rem=%d st=%d i=%d\n",rem, st, i);
    if (st==0) { //before block header
        if (ch=='\xfe') { //byte 0: gyro packet of 4 bytes
            st = 10; pk[i] = ch; i++;
        } else if (ch=='\xff') { //byte 0: data packet of 40 bytes
            st = 20; pk[i] = ch; i++;
        } else {
            i = 0;
        }
    } else if (st==10) {
		if (ch=='\xfe') { // byte 1: gyro packet of 4 bytes
			st = 11; pk[i] = ch; i++;
		} else {
			st = 0; i = 0;
		}
    } else if (st==11) {
             pk[i] = ch; i++;
             if (i>=4) { //gyro packet ready
                s2x(pk,4);
                printf("%s\n",h);
                st = 0; i = 0;
                return(1); //packet ready
             }
    } else if (st==20) {
 		if (ch=='\xff') { // byte 1: gyro packet of 4 bytes
			st = 21; pk[i] = ch; i++;
		} else {
			st = 0; i = 0;
		}
    } else if (st==21) { //bytes 2-39: data packet of 40 bytes
             pk[i] = ch; i++;
             if (i>=40) { //data packet ready
                s2d(pk,40);
                printf("%s\n",h);
                st = 0; i = 0;
                return(1); //packet ready
             }
    }
    return(0);
}

void s2d (char *s, int sl) { //string to hexadecimal string
    for(i=0;i<sl;i=i+2){
        sprintf(&h[i*4],"% 8d",(signed char)s[i]*256+(unsigned char)s[i+1]);
        h[sl*4]='\0';
    }
}

void s2x (char *s, int sl) { //string to d string
    for(i=0;i<sl;i++){
        sprintf(&h[i*2],"%02x",(int)s[i]&0x000000ff);
        h[sl*2]='\0';
    }
}
