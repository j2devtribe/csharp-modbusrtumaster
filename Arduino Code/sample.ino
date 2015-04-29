#include <j2thread.h>
#include <ModbusRTUSlave.h>

u16 _D[100];
u8 _M[50];

ModbusRTUSlave rtu(1, &Serial1);

void setup() {
  pinMode(3, OUTPUT);
  pinMode(4, OUTPUT);
  pinMode(5, INPUT);
  rtu.addWordArea(0x7000, _D, 100);
  rtu.addBitArea(0x1000, _M, 50);
  rtu.begin(9600);
  
  add_thread(new J2ThreadUnit(&comm_loop));
  add_thread(new J2ThreadUnit(&logic_loop));
  add_thread(new J2ThreadUnit(&update_loop));
}

bool comm_loop(J2ThreadUnit* th)
{
  rtu.process();
  th->sleep_micro(10);
  return true; 
}

bool logic_loop(J2ThreadUnit* th)
{
  setBit(_M, 0, !getBit(_M,0));
  digitalWrite(3, getBit(_M,0));
  th->sleep_milli(1000);
  return true; 
}

bool update_loop(J2ThreadUnit* th)
{
  digitalWrite(4, getBit(_M, 1));
  setBit(_M, 2, digitalRead(5));
  _D[0] = analogRead(0);
  th->sleep_milli(1);
  return true; 
}
