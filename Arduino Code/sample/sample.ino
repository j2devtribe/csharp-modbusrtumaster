#include <ModbusRTUSlave.h>

u16 _D[100];
u8 _M[50];
ModbusRTUSlave rtu(1, &Serial1);
u32 prev; 
void setup() {
  pinMode(3, OUTPUT);
  pinMode(4, OUTPUT);
  pinMode(5, INPUT);
  rtu.addWordArea(0x7000, _D, 100);
  rtu.addBitArea(0x1000, _M, 50);
  rtu.begin(9600);
}
 
void loop()
{
  u32 n = millis();
  if(n-prev>=1000 || n<prev)
  {
    setBit(_M, 0, !getBit(_M,0));
    digitalWrite(3, getBit(_M,0));
    prev=n;
  }
  
  digitalWrite(4, getBit(_M, 1));
  setBit(_M, 2, digitalRead(5));
  _D[0] = analogRead(0);

  rtu.process();
}
