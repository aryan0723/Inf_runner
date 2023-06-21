using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Runtime.InteropServices;

namespace TempleRun.Player
{

    [RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        private float initialPlayerSpeed = 4f;
        [SerializeField]
        private float maxPlayerSpeed = 30f;
        [SerializeField]
        private float playerSpeedIncrease = 0.2f;
        [SerializeField]
        private float jumpHeight = 3f;
        [SerializeField]
        private float initialGravity = -9.8f;
        [SerializeField]
        private float scoreMultiplier = 5f;
        
        [SerializeField]
        private LayerMask groundLayer;
        [SerializeField]
        private LayerMask turnLayer;
        [SerializeField]
        private LayerMask obstacleLayer;

        private float playerSpeed;
        private float gravity;
        private Vector3 movementDirection = Vector3.forward;
        private Vector3 playerVelocity;

        private PlayerInput playerInput;
        private InputAction turnAction;
        private InputAction jumpAction;
        private InputAction slideAction;
    

        private CharacterController characterController;

        [SerializeField]
        private UnityEvent<Vector3> turnEvent;
        [SerializeField]
        private UnityEvent<int> gameOverEvent;
        [SerializeField]
        private UnityEvent<int> scoreUpdateEvent;

        private bool sliding = false;

        private float score = 0;
        

        private Animation anim;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            characterController = GetComponent<CharacterController>();
            turnAction = playerInput.actions["Turn"];
            jumpAction = playerInput.actions["Jump"];
            slideAction = playerInput.actions["Slide"];
            
            anim = GetComponentInChildren<Animation>();


        }
        private void OnEnable()
        {
            turnAction.performed += PlayerTurn;
            slideAction.performed += PlayerSlide;
            jumpAction.performed += PlayerJump;
        
        }
        private void OnDisable()
        {
            turnAction.performed -= PlayerTurn;
            slideAction.performed -= PlayerSlide;
            jumpAction.performed -= PlayerJump;
         
        }
        private void Start()
        {
            gravity = initialGravity;
            playerSpeed = initialPlayerSpeed;
            anim.Play("Run");
        }
        private void PlayerTurn(InputAction.CallbackContext context)
        {
            Vector3? turnPosition =  CheckTurn(context.ReadValue<float>());
            if(!turnPosition.HasValue)
            {
                //anim.Play("Dizzy");
                //GameOver();
                return;
            }
            Vector3 targetDirection = Quaternion.AngleAxis(90 * context.ReadValue<float>(), Vector3.up) * movementDirection;

            turnEvent.Invoke(targetDirection);
            Turn(context.ReadValue<float>(),turnPosition.Value);
        }
        private Vector3? CheckTurn(float turnValue)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.1f, turnLayer);
            if (hitColliders.Length > 0)
            {
                Tile tile = hitColliders[0].transform.parent.GetComponent<Tile>();
                TileType type = tile.type;
                if((type==TileType.LEFT && turnValue == -1) || (type==TileType.RIGHT && turnValue == 1) || (type == TileType.SIDEWAYS))
                {
                    return tile.pivot.position;
                }
            }
            return null;
        }

        private void Turn(float turnValue, Vector3 turnPosition) {
            Vector3 tempPlayerPosition = new Vector3(turnPosition.x, turnPosition.y, turnPosition.z);
            characterController.enabled = false; 
            transform.position = tempPlayerPosition;
            characterController.enabled = true;

            Quaternion targetRotation = transform.rotation * Quaternion.Euler(0, 90 * turnValue, 0); 
            transform.rotation = targetRotation;
            movementDirection = transform.forward.normalized;
        }
        private void PlayerJump(InputAction.CallbackContext context)
        {

            if (IsGrounded())
            {
                Debug.Log("Jump");
                anim.Play("Runtojumpspring");
                playerVelocity.y += Mathf.Sqrt(jumpHeight * gravity * -3f);
                characterController.Move(playerVelocity * Time.deltaTime);
               
            }
        }
        private void PlayerSlide(InputAction.CallbackContext context)
        {
            if(!sliding && IsGrounded())
            {
                StartCoroutine(Slide());
            }
        }
        private IEnumerator Slide()
        {
            sliding = true;
            Vector3 originalCharacterControllerCenter = characterController.center;
            Vector3 newCharacterControllerCenter = originalCharacterControllerCenter;

            characterController.height /= 2;
            newCharacterControllerCenter.y -= characterController.height / 2 ;
            characterController.center = newCharacterControllerCenter;


            
            anim.Play("Runtoslide");
            yield return new WaitForSeconds((float) 1.250);
           

            characterController.height *= 2;
            characterController.center = originalCharacterControllerCenter;
            sliding = false;

        }
        private void Update()
        {
            if(transform.position.y < 0)
            {
                GameOver();
                return;
            }
            if (!anim.isPlaying)
            {
                anim.Play("Run");
            }

            //score updation 
            score += scoreMultiplier * Time.deltaTime;
            scoreUpdateEvent.Invoke((int)score);


            characterController.Move(transform.forward * playerSpeed * Time.deltaTime);

            if (IsGrounded() && playerVelocity.y < 0)
            {
                playerVelocity.y = 0;
            }
            playerVelocity.y += gravity * Time.deltaTime;
            characterController.Move(playerVelocity * Time.deltaTime);

            if (playerSpeed < maxPlayerSpeed)
            {
                playerSpeed += Time.deltaTime * playerSpeedIncrease;
               gravity = initialGravity - 0.1f*playerSpeed;
                
            }
            
        }
        
        private bool IsGrounded(float length = 0.2f)
        {
            Vector3 raycastOriginFirst = transform.position;
            raycastOriginFirst.y -= characterController.height / 2f;
            raycastOriginFirst.y += 0.1f;// offset 

            Vector3 raycastOriginSecond = raycastOriginFirst;
            raycastOriginFirst -= transform.forward * 0.2f;
            raycastOriginSecond += transform.forward * 0.2f;

           // Debug.DrawLine(raycastOriginFirst, Vector3.down, Color.green, 10f);
            //Debug.DrawLine(raycastOriginSecond, Vector3.down, Color.red, 10f);

            if (Physics.Raycast(raycastOriginFirst, Vector3.down, out RaycastHit hit, length, groundLayer) ||
               Physics.Raycast(raycastOriginSecond, Vector3.down, out RaycastHit hit2, length, groundLayer))
            {
                return true;
            }
            return false;
        }

        private void GameOver()
        {
            Debug.Log("Game Over");
            gameOverEvent.Invoke((int)score);
            gameObject.SetActive(false);
        }
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if( ( (1<<hit.collider.gameObject.layer ) & obstacleLayer) != 0)
            {
                GameOver();
            }
        }
    }
}
