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
 [SerializeField] private float arenaWidth = 8f;
 [SerializeField] private float arenaHeight = 4f;
 // Mode opcional per al mapa v0.1 amb raycasts en forma de con.
 [SerializeField] private bool useConeRaycasts = false;
 [SerializeField] private float rayDistance = 8f;
 [SerializeField] private int rayCount = 5;
 [SerializeField] private float rayConeAngle = 90f;
 [SerializeField] private float reachDistance = 1.25f;
 // Refer�ncia al Rigidbody2D de l'agent.
 // El necessitem per moure'l amb f�sica 2D.
 private Rigidbody2D rb;
 private Rigidbody2D targetRb;
 private SpriteRenderer floorRenderer;
 private Vector2 lastMoveDirection = Vector2.right;
 private bool seesInterestingTarget;
 private bool hasSeenInterestingTarget;
 // Aquesta funci� s'executa una vegada al principi.
 // Aqu� guardem la refer�ncia al Rigidbody2D.
 public override void Initialize()
 {
 PrepareConeMode();
 rb = GetOrCreateAgentBody();
 targetRb = target != null ? target.GetComponent<Rigidbody2D>() : null;
 }
 // Aquesta funci� s'executa cada vegada que comen�a un episodi nou.
 // Un episodi �s un "intent" complet de l'agent.
 public override void OnEpisodeBegin()
 {
 PrepareConeMode();
 targetRb = target != null ? target.GetComponent<Rigidbody2D>() : null;
 hasSeenInterestingTarget = false;
 seesInterestingTarget = false;
 lastMoveDirection = Vector2.right;
 // Reiniciem la velocitat lineal i angular perqu� no arrossegui moviment
 // de l'episodi anterior.
 rb.linearVelocity = Vector2.zero;
 rb.angularVelocity = 0f;
 if (targetRb != null)
 {
 targetRb.linearVelocity = Vector2.zero;
 targetRb.angularVelocity = 0f;
 }
 if (useConeRaycasts && floorRenderer != null && transform.parent == null && (target == null || target.parent == null))
 {
 transform.position = GetRandomWorldPosition();
 Physics2D.SyncTransforms();
 if (target != null)
 {
 target.position = GetRandomWorldPosition(transform.position);
 Physics2D.SyncTransforms();
 }
 return;
 }
 // Col�loquem l'agent en una posici� aleat�ria dins de l'arena.
 // En 2D treballem amb X i Y.
 transform.localPosition = new Vector3(
 Random.Range(-arenaWidth/2, arenaWidth/2),
 Random.Range(-arenaHeight/2, arenaHeight/2),
 0f
 );
 // Col�loquem tamb� el target en una posici� aleat�ria.
 // Aix� obliga l'agent a aprendre a buscar-lo en lloc de memoritzar una posici� fixa.
 if (target != null)
 {
 target.localPosition = new Vector3(
 Random.Range(-arenaWidth/2, arenaWidth/2),
 Random.Range(-arenaHeight/2, arenaHeight/2),
 0f
 );
 }
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
 if (!useConeRaycasts)
 {
 // Posici� X i Y del target
 sensor.AddObservation(target != null ? target.localPosition.x : 0f);
 sensor.AddObservation(target != null ? target.localPosition.y : 0f);
 // Vector relatiu entre target i agent.
 // Aix� ajuda molt, perqu� diu directament "cap on est� l'objectiu".
 Vector3 relative = target != null ? target.localPosition - transform.localPosition : Vector3.zero;
 sensor.AddObservation(relative.x);
 sensor.AddObservation(relative.y);
 return;
 }
 // Afegim la direccio principal per saber cap on mira el con.
 Vector2 forward = lastMoveDirection.sqrMagnitude > 0.001f ? lastMoveDirection.normalized : Vector2.right;
 sensor.AddObservation(forward.x);
 sensor.AddObservation(forward.y);
 seesInterestingTarget = false;
 int totalRays = Mathf.Max(1, rayCount);
 for (int i = 0; i < totalRays; i++)
 {
 float t = totalRays == 1 ? 0.5f : (float)i / (totalRays - 1);
 float angle = Mathf.Lerp(-rayConeAngle * 0.5f, rayConeAngle * 0.5f, t);
 Vector2 direction = RotateVector(forward, angle);
 RaycastHit2D hit = GetConeHit(direction);
 float normalizedDistance = 1f;
 float hitWall = 0f;
 float hitTarget = 0f;
 float hitPlayer = 0f;
 if (hit.collider != null)
 {
 normalizedDistance = Mathf.Clamp01(hit.distance / rayDistance);
 if (IsTargetTransform(hit.transform))
 {
 hitTarget = 1f;
 seesInterestingTarget = true;
 }
 else if (IsOtherAgent(hit.transform))
 {
 hitPlayer = 1f;
 }
 else
 {
 hitWall = 1f;
 }
 }
 sensor.AddObservation(normalizedDistance);
 sensor.AddObservation(hitWall);
 sensor.AddObservation(hitTarget);
 sensor.AddObservation(hitPlayer);
 }
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
 if (dir != Vector2.zero)
 {
 lastMoveDirection = dir.normalized;
 }
 // Apliquem una for�a instant�nia a l'agent en la direcci� triada.
 // En 2D fem servir ForceMode2D.Impulse per obtenir un efecte semblant
 // a un canvi r�pid de velocitat.
 //rb.AddForce(dir * moveSpeed, ForceMode2D.Impulse);
 rb.linearVelocity = dir * moveSpeed;
 // Petita penalitzaci� cada pas.
 // Aix� fa que l'agent no perdi el temps i intenti arribar r�pid al target.
 AddReward(useConeRaycasts ? -0.0002f : -0.001f);
 if (useConeRaycasts && seesInterestingTarget && !hasSeenInterestingTarget)
 {
 AddReward(0.2f);
 hasSeenInterestingTarget = true;
 }
 if (useConeRaycasts && ReachedInterestingTarget())
 {
 return;
 }
 // Si l'agent surt massa lluny de la zona de joc, considerem que ha fallat.
 if (IsOutsideArena())
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
 if (IsTargetTransform(collision.transform) || (!useConeRaycasts && IsOtherAgent(collision.transform)))
 {
 // Recompensa positiva per haver arribat a l'objectiu
 AddReward(1f);
 // Acabem l'episodi perqu� el repte ja s'ha resolt
 EndEpisode();
 }
 }

 private void PrepareConeMode()
 {
 if (!useConeRaycasts)
 {
 return;
 }
 target = ResolveTarget();
 floorRenderer = ResolveFloorRenderer();
 PrepareSceneRaycastColliders();
 Physics2D.SyncTransforms();
 }

 private Rigidbody2D GetOrCreateAgentBody()
 {
 Rigidbody2D body = GetComponent<Rigidbody2D>();
 if (body == null && useConeRaycasts)
 {
 body = gameObject.AddComponent<Rigidbody2D>();
 }
 if (body != null)
 {
 body.gravityScale = 0f;
 body.angularDamping = 0.05f;
 body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
 body.constraints = RigidbodyConstraints2D.FreezeRotation;
 }
 if (useConeRaycasts)
 {
 CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
 if (capsule == null)
 {
 capsule = gameObject.AddComponent<CapsuleCollider2D>();
 }
 capsule.direction = CapsuleDirection2D.Vertical;
 if (capsule.size.sqrMagnitude <= 0.001f)
 {
 capsule.size = new Vector2(1f, 2f);
 }
 }
 return body;
 }

 private Transform ResolveTarget()
 {
 if (target != null)
 {
 return target;
 }
 GameObject taggedTarget = GameObject.FindWithTag("Target");
 if (taggedTarget != null)
 {
 return taggedTarget.transform;
 }
 GameObject namedTarget = GameObject.Find("Target");
 return namedTarget != null ? namedTarget.transform : null;
 }

 private SpriteRenderer ResolveFloorRenderer()
 {
 GameObject floor = GameObject.Find("Floor");
 return floor != null ? floor.GetComponent<SpriteRenderer>() : null;
 }

 private void PrepareSceneRaycastColliders()
 {
 SpriteRenderer[] sceneSprites = FindObjectsByType<SpriteRenderer>();
 for (int i = 0; i < sceneSprites.Length; i++)
 {
 SpriteRenderer sceneSprite = sceneSprites[i];
 if (sceneSprite == null || sceneSprite.transform == transform)
 {
 continue;
 }
 if (!IsWallTransform(sceneSprite.transform) && !IsTargetTransform(sceneSprite.transform) && !IsOtherAgent(sceneSprite.transform))
 {
 continue;
 }
 if (sceneSprite.GetComponent<Collider2D>() == null)
 {
 sceneSprite.gameObject.AddComponent<BoxCollider2D>();
 }
 }
 }

 private Vector3 GetRandomWorldPosition()
 {
 return GetRandomWorldPosition(Vector3.zero, false);
 }

 private Vector3 GetRandomWorldPosition(Vector3 avoidPosition)
 {
 return GetRandomWorldPosition(avoidPosition, true);
 }

 private Vector3 GetRandomWorldPosition(Vector3 avoidPosition, bool shouldAvoidPosition)
 {
 Bounds bounds = floorRenderer != null
 ? floorRenderer.bounds
 : new Bounds(Vector3.zero, new Vector3(arenaWidth, arenaHeight, 0f));
 float margin = 1.5f;
 for (int i = 0; i < 40; i++)
 {
 Vector2 candidate = new Vector2(
 Random.Range(bounds.min.x + margin, bounds.max.x - margin),
 Random.Range(bounds.min.y + margin, bounds.max.y - margin)
 );
 if (shouldAvoidPosition && Vector2.Distance(candidate, avoidPosition) < reachDistance * 3f)
 {
 continue;
 }
 if (!IsBlockedPosition(candidate))
 {
 return new Vector3(candidate.x, candidate.y, 0f);
 }
 }
 return new Vector3(bounds.center.x, bounds.center.y, 0f);
 }

 private bool IsBlockedPosition(Vector2 position)
 {
 Collider2D[] hits = Physics2D.OverlapCircleAll(position, 0.6f);
 for (int i = 0; i < hits.Length; i++)
 {
 Collider2D hit = hits[i];
 if (hit == null || hit.transform == transform)
 {
 continue;
 }
 if (IsWallTransform(hit.transform) || IsTargetTransform(hit.transform) || IsOtherAgent(hit.transform))
 {
 return true;
 }
 }
 return false;
 }

 private RaycastHit2D GetConeHit(Vector2 direction)
 {
 RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, rayDistance);
 for (int i = 0; i < hits.Length; i++)
 {
 if (hits[i].collider == null || hits[i].transform == transform)
 {
 continue;
 }
 return hits[i];
 }
 return default;
 }

 private Vector2 RotateVector(Vector2 direction, float angle)
 {
 return Quaternion.Euler(0f, 0f, angle) * direction;
 }

 private bool ReachedInterestingTarget()
 {
 if (target != null && Vector2.Distance(transform.position, target.position) <= reachDistance)
 {
 AddReward(1f);
 EndEpisode();
 return true;
 }
 if (useConeRaycasts)
 {
 return false;
 }
 SimpleAgent[] sceneAgents = FindObjectsByType<SimpleAgent>();
 for (int i = 0; i < sceneAgents.Length; i++)
 {
 SimpleAgent sceneAgent = sceneAgents[i];
 if (sceneAgent == null || sceneAgent.transform == transform)
 {
 continue;
 }
 if (Vector2.Distance(transform.position, sceneAgent.transform.position) <= reachDistance)
 {
 AddReward(1f);
 EndEpisode();
 return true;
 }
 }
 return false;
 }

 private bool IsOutsideArena()
 {
 if (useConeRaycasts && floorRenderer != null && transform.parent == null)
 {
 Bounds bounds = floorRenderer.bounds;
 Vector3 position = transform.position;
 return position.x < bounds.min.x - 2f ||
 position.x > bounds.max.x + 2f ||
 position.y < bounds.min.y - 2f ||
 position.y > bounds.max.y + 2f;
 }
 return Mathf.Abs(transform.localPosition.x) > arenaWidth / 2f + 2f ||
 Mathf.Abs(transform.localPosition.y) > arenaHeight / 2f + 2f;
 }

 private bool IsTargetTransform(Transform candidate)
 {
 return candidate != null &&
 (candidate == target || candidate.name == "Target" || candidate.CompareTag("Target"));
 }

 private bool IsOtherAgent(Transform candidate)
 {
 return candidate != null &&
 candidate != transform &&
 (candidate.GetComponent<Agent>() != null || candidate.name.StartsWith("Agent"));
 }

 private bool IsWallTransform(Transform candidate)
 {
 return candidate != null && candidate.name.StartsWith("Wall");
 }
}
