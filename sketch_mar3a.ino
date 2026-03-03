#include <WiFi.h>
#include <WebServer.h>

// --- Pin Definitions ---
#define TRIG_PIN 5
#define ECHO_PIN 19
#define LED_PIN 2
#define SPEAKER_PIN 4
#define ALARM_DISTANCE 20

// --- YOUR ACTUAL WIFI CREDENTIALS ---
const char* ssid = "Redmi Note 11T 5G";
const char* password = "********";  // Change to real password
// -------------------------------------

WebServer server(80);

float currentDistance = 0;
bool alarmState = false;

void setup() {
  Serial.begin(115200);
  Serial.println("\n=== STARTING Distance Alarm ===\n");
  
  pinMode(TRIG_PIN, OUTPUT);
  pinMode(ECHO_PIN, INPUT);
  pinMode(LED_PIN, OUTPUT);
  pinMode(SPEAKER_PIN, OUTPUT);
  
  digitalWrite(TRIG_PIN, LOW);
  
  // Connect to WiFi
  WiFi.begin(ssid, password);
  Serial.print("Connecting to WiFi: ");
  Serial.println(ssid);
  
  // Wait up to 20 seconds (instead of 10)
  unsigned long timeoutStart = millis();
  while (WiFi.status() != WL_CONNECTED && millis() - timeoutStart < 20000) {
    delay(500);
    Serial.print(".");
  }
  
  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("\n✅ WiFi Connected!");
    Serial.print("IP Address: http://");
    Serial.println(WiFi.localIP());
    
    server.on("/", handleRoot);
    server.on("/distance", handleDistance);
    server.begin();
    Serial.println("HTTP server started\n");
  } else {
    Serial.println("\n❌ WiFi Connection Failed!\n");
    Serial.println("Checking:");
    Serial.println("1. Are you using 2.4GHz WiFi? (Not 5GHz)");
    Serial.println("2. Is WiFi hidden?");
    Serial.println("3. Are credentials exactly correct?");
    Serial.println("4. Is your router blocking new devices?\n");
  }
}

void loop() {
  server.handleClient();
  
  // Measure distance
  digitalWrite(TRIG_PIN, LOW);
  delayMicroseconds(2);
  digitalWrite(TRIG_PIN, HIGH);
  delayMicroseconds(10);
  digitalWrite(TRIG_PIN, LOW);
  
  long duration = pulseIn(ECHO_PIN, HIGH, 30000);
  currentDistance = duration * 0.034 / 2;
  
  // Alarm logic
  if (currentDistance > 0 && currentDistance < ALARM_DISTANCE) {
    digitalWrite(LED_PIN, HIGH);
    tone(SPEAKER_PIN, 2000);
    alarmState = true;
  } else {
    digitalWrite(LED_PIN, LOW);
    noTone(SPEAKER_PIN);
    alarmState = false;
  }
  
  delay(100);
}

void handleRoot() {
  String html = "<html><body><h1>Smart Distance Alarm</h1>";
  html += "<h2>Distance: " + String(currentDistance) + " cm</h2>";
  html += alarmState ? "<h2 style='color:red'>ALARM!</h2>" : "<h2 style='color:green'>Safe</h2>";
  html += "</body></html>";
  server.send(200, "text/html", html);
}

void handleDistance() {
  String json = "{\"distance\":" + String(currentDistance) + ",\"alarm\":" + (alarmState ? "true" : "false") + "}";
  server.send(200, "application/json", json);
}