// the setup function runs once when you press reset or power the board

#include <Arduino.h>

#include <SPI.h>

#include <Adafruit_GFX.h>
#include <Waveshare_ILI9486.h>
#include <Fonts/FreeSansBold12pt7b.h>

// Assign human-readable names to some common 16-bit color values:
#define	BLACK   0x0000
#define	BLUE    0x001F
#define	RED     0xF800
#define	GREEN   0x07E0
#define CYAN    0x07FF
#define MAGENTA 0xF81F
#define YELLOW  0xFFE0
#define WHITE   0xFFFF

const byte numChars = 32;
char receivedChars[32];
int recievedINT;
boolean newData = false;

int fontStep = 40;

int lastLines = 0;
uint16_t ShiftColor = GREEN;

uint16_t DeltaColor = GREEN;
bool ColorChange = false;

namespace
{
    Waveshare_ILI9486 Waveshield;
}

void setRPM(char RPM1, char RPM2)
{
      // Don't draw same rpm again
      if(RPM1 == 'P'){
        return;
      }
      
      String SRPM1 = String(RPM1);
      String SRPM2 = String(RPM2);
      
      if(SRPM2 == "P"){SRPM2 = " ";}
      
      //Draw
      Waveshield.setCursor(480/2,150);
      Waveshield.print(SRPM1+SRPM2);
}
void setGear(char gear)
{     
      // Don't draw same gear again
      if(gear == 'X'){
        return;
      }
      
      Waveshield.setCursor(480/2-12,85);
      Waveshield.print(gear);
}

void setDelta()
{
    if(receivedChars[3] == '-'){DeltaColor = GREEN; ColorChange = true;}
    if(receivedChars[3] == '+'){DeltaColor = RED; ColorChange = true;}

    Waveshield.setTextColor(DeltaColor, BLACK);
    
    int k = 0;
    for(int i=4; i<8; i++){
      
      if(receivedChars[i] != 'S' | ColorChange == true){
             
        Waveshield.setCursor(315+k*fontStep,210);
        Waveshield.print(receivedChars[i]);
      }

      k++;
      
    }

    ColorChange = false;
    
}

void setCarSpeed()
{
    Waveshield.setTextColor(WHITE, BLACK);
    int k = 0; 
    for(int i=8; i < 11; i++){

       if(receivedChars[i] != 'P'){

        Waveshield.setCursor(30+k*fontStep,78);
        Waveshield.print(receivedChars[i]);
       }
      k++;
    }
}
void setFlag()
{
    char flag = receivedChars[11];

    // USE SWITCH STATEMENTS
    if(flag != 'E'){
      
      if(flag == '0'){Waveshield.fillRect(200, 190, 100, 100, BLACK);}
      if(flag == '1'){Waveshield.fillRect(200, 190, 100, 100, BLUE);}
      if(flag == '2'){Waveshield.fillRect(200, 190, 100, 100, MAGENTA);}
      if(flag == '3'){Waveshield.fillRect(200, 190, 100, 100, YELLOW);}
      if(flag == '4'){Waveshield.fillRect(200, 190, 100, 100, CYAN);}
      if(flag == '5'){Waveshield.fillRect(200, 190, 100, 100, WHITE);}
    }
}

void setFuel()
{
    Waveshield.setTextColor(WHITE, BLACK);    
    int k = 0; 
    for(int i=12; i < 15; i++){

       if(receivedChars[i] != 'F'){

        Waveshield.setCursor(0+k*fontStep,210);
        Waveshield.print(receivedChars[i]);
       }
      k++;
    }
}

void setBBias()
{
    int k = 0; 
    for(int i=15; i < 19; i++){

       if(receivedChars[i] != 'B'){

        Waveshield.setCursor(315+k*fontStep,78);
        Waveshield.print(receivedChars[i]);
       }
      k++;
    }
}

void setShift()
{ 

}

void setup() 
{
    Serial.begin(115200); //Starts the serial connection with 115200 Buad Rate   
    SPI.begin();
    Waveshield.begin();

    // Using basic font because Adafruit funny business with fonts.
    // New fotns require drawing rectangles to wipe off old character and thiss will be slow.
    Waveshield.setRotation(1);
    Waveshield.setTextSize(7);

    // Treated as special case. Drawing over old text -> text disappears from buffer.
    Waveshield.setTextColor(WHITE, BLACK);

    // Draw the white frame of the dash

    // Draw horisontal lines
    Waveshield.drawFastHLine(0,185,480, WHITE);
    Waveshield.drawFastHLine(0,50,480, WHITE);
    Waveshield.drawFastHLine(0,319,480, WHITE);

    // Draw vertical lines
    Waveshield.drawFastVLine(190,50,319, WHITE);
    Waveshield.drawFastVLine(310,50,319, WHITE);

    //Draw % sign for fuel percent
    Waveshield.setCursor(3*fontStep,210);
    Waveshield.print("%");
}

// the loop function runs over and over again until power down or reset
void loop()
{
    Waveshield.setTextColor(WHITE, BLACK);
    recvWithStartEndMarkers();
    //showNewData();
    if(newData == true)
    {
        // Gear with larger font size        
        Waveshield.setTextSize(9);
        setGear(receivedChars[2]);
        Waveshield.setTextSize(7);
        
        setDelta();
        setCarSpeed();
        setFlag();
        setFuel();
        setBBias();
        setShift();
        
        newData = false;
    }
}

void recvWithStartEndMarkers() {
    static boolean recvInProgress = false;
    static byte ndx = 0;
    char startMarker = '<';
    char endMarker = '>';
    char rc;
 
    while (Serial.available() > 0 && newData == false) {
        rc = Serial.read();

        if (recvInProgress == true) {
            if (rc != endMarker) {
                receivedChars[ndx] = rc;
                ndx++;
                if (ndx >= numChars) {
                    ndx = numChars - 1;
                }
            }
            else {
                receivedChars[ndx] = '\0'; // terminate the string
                recvInProgress = false;
                ndx = 0;
                newData = true;
            }
        }

        else if (rc == startMarker) {
            recvInProgress = true;
        }
    }
}

void showNewData() {
    if (newData == true) {
        Waveshield.setCursor(0,0);
        Waveshield.print(receivedChars);
        newData = false;
    }
}
