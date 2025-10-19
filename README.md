## Overview

The system consists of:

### 1. **Server**
Located in the `serverUDP` folder, this part handles:
- **Serial acquisition** of sensor data from the glove using `rs232kk01.c`
- **Continuous UDP broadcasting** of formatted data to the headset using `sender.c`

The IP address of the headset must be manually updated in `sender.c` since it changes.

### 2. **Client**
The Unity project
