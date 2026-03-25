using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
// Aquesta classe hereta de Agent.
// Aix� vol dir que Unity ML-Agents la tractar� com un agent entrenable.
public class SimpleAgent : Agent
{
 // Refer�ncia al target (l'objectiu que l'agent ha d'anar a tocar).
 // Es podr� assignar des de l'Inspector de Unity.
 [SerializeField] private Transform target;

 // Velocitat de moviment de l'agent.
 // Com m�s alt sigui aquest valor, m�s for�a s'aplicar� en cada acci�.
 [SerializeField] private float moveSpeed = 5f;
 // L�mit de l'arena per generar posicions aleat�ries.
 // S'utilitza per reiniciar l'agent i el target dins d'una zona concreta.
 [SerializeField] private float arenaLimit = 4f;
 // Refer�ncia al Rigidbody2D de l'agent.
 // El necessitem per moure'l amb f�sica 2D.
 private Rigidbody2D rb;
 private Rigidbody2D targetRb;
 // Aquesta funci� s'executa una vegada al principi.
 // Aqu� guardem la refer�ncia al Rigidbody2D.
 public override void Initialize()
 {
 rb = GetComponent<Rigidbody2D>();
 targetRb = target.GetComponent<Rigidbody2D>();
 }
 // Aquesta funci� s'executa cada vegada que comen�a un episodi nou.
 // Un episodi �s un "intent" complet de l'agent.
 public override void OnEpisodeBegin()
 {
 // Reiniciem la velocitat lineal i angular perqu� no arrossegui moviment
 // de l'episodi anterior.
 rb.linearVelocity = Vector2.zero;
 rb.angularVelocity = 0f;
 if (targetRb != null)
 {
 targetRb.linearVelocity = Vector2.zero;
 targetRb.angularVelocity = 0f;
 }
 // Col�loquem l'agent en una posici� aleat�ria dins de l'arena.
 // En 2D treballem amb X i Y.
 transform.localPosition = new Vector3(
 Random.Range(-arenaLimit, arenaLimit),
 Random.Range(-arenaLimit, arenaLimit),
 0f
 );
 // Col�loquem tamb� el target en una posici� aleat�ria.
 // Aix� obliga l'agent a aprendre a buscar-lo en lloc de memoritzar una posici� fixa.
 target.localPosition = new Vector3(
 Random.Range(-arenaLimit, arenaLimit),
 Random.Range(-arenaLimit, arenaLimit),
 0f
 );
 }
 // Aqu� definim quina informaci� veu l'agent.
 // Aquestes dades s�n les "observacions" que el model far� servir per decidir.
 public override void CollectObservations(VectorSensor sensor)
 {
 // Posici� X i Y de l'agent
 sensor.AddObservation(transform.localPosition.x);
 sensor.AddObservation(transform.localPosition.y);
 // Velocitat X i Y de l'agent
 sensor.AddObservation(rb.linearVelocity.x);
 sensor.AddObservation(rb.linearVelocity.y);
 // Posici� X i Y del target
 sensor.AddObservation(target.localPosition.x);
 sensor.AddObservation(target.localPosition.y);
 // Vector relatiu entre target i agent.
 // Aix� ajuda molt, perqu� diu directament "cap on est� l'objectiu".
 Vector3 relative = target.localPosition - transform.localPosition;
 sensor.AddObservation(relative.x);
 sensor.AddObservation(relative.y);
 }
 // Aquesta funci� rep l'acci� decidida pel model.
 // Aqu� �s on convertim aquesta acci� en moviment real dins del joc.
 public override void OnActionReceived(ActionBuffers actions)
 {
 // Agafem la primera acci� discreta.
 // En aquest exemple:
 // 0 = no fer res
 // 1 = amunt
 // 2 = avall
 // 3 = esquerra
 // 4 = dreta
 int action = actions.DiscreteActions[0];
 // Direcci� inicial: cap moviment
 Vector2 dir = Vector2.zero;
 // Tradu�m el n�mero de l'acci� en una direcci� de moviment.
 switch (action)
 {
 case 1: dir = Vector2.up; break;
 case 2: dir = Vector2.down; break;
 case 3: dir = Vector2.left; break;
 case 4: dir = Vector2.right; break;
 }
 // Apliquem una for�a instant�nia a l'agent en la direcci� triada.
 // En 2D fem servir ForceMode2D.Impulse per obtenir un efecte semblant
 // a un canvi r�pid de velocitat.
 //rb.AddForce(dir * moveSpeed, ForceMode2D.Impulse);
 rb.linearVelocity = dir * moveSpeed;
 // Petita penalitzaci� cada pas.
 // Aix� fa que l'agent no perdi el temps i intenti arribar r�pid al target.
 AddReward(-0.001f);
 // Si l'agent surt massa lluny de la zona de joc, considerem que ha fallat.
 if (Mathf.Abs(transform.localPosition.x) > arenaLimit + 2f ||
 Mathf.Abs(transform.localPosition.y) > arenaLimit + 2f)
 {
 // Penalitzaci� per error greu
 AddReward(-1f);
 // Acabem l'episodi i en comen�ar� un de nou
 EndEpisode();
 }
 }
 // Heuristic serveix per controlar manualment l'agent.
 // Va molt b� per provar si les accions estan ben connectades abans d'entrenar.
 public override void Heuristic(in ActionBuffers actionsOut)
 {
 var a = actionsOut.DiscreteActions;
 // Per defecte, cap acci�
 a[0] = 0;
 // Si es prem una tecla, assignem l'acci� corresponent
 if (Input.GetKey(KeyCode.UpArrow)) a[0] = 1;
 else if (Input.GetKey(KeyCode.DownArrow)) a[0] = 2;
 else if (Input.GetKey(KeyCode.LeftArrow)) a[0] = 3;
 else if (Input.GetKey(KeyCode.RightArrow)) a[0] = 4;
 }
 // Aquesta funci� es crida quan l'agent col�lisiona amb un altre objecte en 2D.
 private void OnCollisionEnter2D(Collision2D collision)
 {
 // Si toca el target, vol dir que ha tingut �xit.
 if (collision.transform.CompareTag("Target"))
 {
 // Recompensa positiva per haver arribat a l'objectiu
 AddReward(1f);
 // Acabem l'episodi perqu� el repte ja s'ha resolt
 EndEpisode();
 }
 }
}