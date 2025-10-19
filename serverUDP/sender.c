#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <ws2tcpip.h>
#include <stdio.h>
#include <string.h>
#include <ctype.h> // Per isspace()
#include <windows.h> // Per Sleep()

#pragma comment(lib, "Ws2_32.lib")

#define SERVER_PORT 5052
#define BUFFER_SIZE 512

// Funzione per rimuovere spazi multipli e spazi iniziali/finali
void remove_extra_spaces(char *str) {
    char *dest = str;
    int in_space = 0;

    // Salta gli spazi iniziali
    while (isspace((unsigned char)*str)) {
        str++;
    }

    // Rimuovi spazi multipli e copia i caratteri
    while (*str != '\0') {
        if (!isspace((unsigned char)*str) || (in_space == 0)) {
            *dest++ = *str;
        }
        in_space = isspace((unsigned char)*str);
        str++;
    }

    // Rimuovi eventuale spazio finale
    if (dest > str && isspace((unsigned char)*(dest - 1))) {
        dest--;
    }

    *dest = '\0'; // Termina la stringa
}

int main() {
    WSADATA wsaData;
    SOCKET serverSocket;
    struct sockaddr_in serverAddr;
    char buffer[BUFFER_SIZE];
    char formattedBuffer[BUFFER_SIZE + 2];  // Spazio aggiuntivo per le parentesi quadre

    // Inizializza Winsock
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
        printf("Errore nella inizializzazione di Winsock.\n");
        return 1;
    }

    // Crea un socket UDP
    serverSocket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
    if (serverSocket == INVALID_SOCKET) {
        printf("Errore nella creazione del socket: %d\n", WSAGetLastError());
        WSACleanup();
        return 1;
    }

    // Configura l'indirizzo del server
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = inet_addr("192.168.137.1"); // IP pc
    serverAddr.sin_port = htons(SERVER_PORT);

    printf("Server attivo sulla porta %d...\n", SERVER_PORT);

    FILE *pipe = popen("rs232kk01.exe", "r");
    if (pipe == NULL) {
        printf("Errore nell'aprire la pipe verso il generatore di stringhe.\n");
        closesocket(serverSocket);
        WSACleanup();
        return 1;
    }

    while (1) {
        if (fgets(buffer, BUFFER_SIZE, pipe) != NULL) {
            // Rimuovi eventuali caratteri di nuova linea
            buffer[strcspn(buffer, "\n")] = '\0';

            // Rimuovi spazi multipli e spazi iniziali/finali
            remove_extra_spaces(buffer);

            // Aggiungi parentesi quadre e formatta la stringa
            snprintf(formattedBuffer, BUFFER_SIZE + 2, "[%s]", buffer);

            // Sostituisci gli spazi singoli con le virgole, partendo dall'indice 2
            for (int i = 2; formattedBuffer[i] != '\0'; i++) {
                if (formattedBuffer[i] == ' ') {
                    formattedBuffer[i] = ',';
                }
            }

            struct sockaddr_in clientAddr;
            clientAddr.sin_family = AF_INET;
            clientAddr.sin_port = htons(SERVER_PORT);
            clientAddr.sin_addr.s_addr = inet_addr("192.168.137.240"); //IP visore
            //clientAddr.sin_addr.s_addr = inet_addr("127.0.0.1"); //IP se vuoi mandare al pc stesso

            // Invia il messaggio formattato
            if (sendto(serverSocket, formattedBuffer, (int)strlen(formattedBuffer), 0, (struct sockaddr*)&clientAddr, sizeof(clientAddr)) == SOCKET_ERROR) {
                printf("Errore nell'invio del messaggio: %d\n", WSAGetLastError());
            } else {
                printf("%s\n", formattedBuffer);
            }
        } else {
            printf("Errore nella lettura dalla pipe.\n");
            break;
        }
        
    }

    pclose(pipe);
    closesocket(serverSocket);
    WSACleanup();

    return 0;
}
