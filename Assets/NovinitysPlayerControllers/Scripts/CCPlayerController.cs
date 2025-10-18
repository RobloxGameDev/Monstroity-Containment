using UnityEditor.Timeline;
using UnityEngine;

namespace NovinitysPlayerControllers {
    [RequireComponent(typeof(CharacterController))]
    public class CCPlayerController : MonoBehaviour
    {
        // Required Unity components
        [Header("Components")]

        CharacterController thisController;

        [SerializeField] private Camera cam;

        _3DPlayerControllerActions inputActions;

        // Values stored for use
        [Header("Values")]

        [Tooltip("The speed the player walks at")]
        [SerializeField] private float walkSpeed;

        [Tooltip("The speed the player runs at")]
        [SerializeField] private float runSpeed;

        [Tooltip("The speed the player crouches at")]
        [SerializeField] private float crouchSpeed;

        private float curSpeed;

        [Tooltip("A value that the player's speed is multiplied by")]
        public float speedMultiplier = 1f;

        [Tooltip("The camera's FOV")]
        public float targetFOV = 66f;

        // sprintTime is used to determine how long the player can sprint for
        // So in this case, it is 7 seconds.
        [Tooltip("How long, in seconds, the player should be able to sprint for")]
        public float sprintTime = 7f;

        // The player's current stamina
        private float stamina;

        // Camera speed

        [Tooltip("How fast the camera rotates")]
        [SerializeField] private float sensitivity = 1.0f;


        [Tooltip("The force of the player's jump")]
        [SerializeField] private float jumpForce;

        [Tooltip("How much stamina jumping takes away")]
        public float jumpStaminaTax = 0.1f;

        // Crouching stuff
        [Header("Height")]
        
        [Tooltip("How tall the player should be while crouching")]
        [SerializeField] private float crouchHeight = 1f;

        [Tooltip("The default height of the player - used for resetting crouch")]
        [SerializeField] private float baseHeight = 2f;

        [Tooltip("All layers that are not the player")]
        [SerializeField] private LayerMask notPlayerMask;

        // Gravity stuff
        [Header("Gravity")]

        private Vector3 velocity;

        private float yVelocityUncapped;

        [Tooltip("The amount of gravity the player has")]
        public float gravity = -9.81f;

        [Header("Toggles")]

        [Tooltip("Whether or not the player should be able to sprint")]
        [SerializeField] private bool allowSprint = true;

        [Tooltip("Whether or not the player should be able to crouch")]
        [SerializeField] private bool allowCrouch = true;

        [Tooltip("Whether or not the player should be able to jump")]
        [SerializeField] private bool allowJump = true;

        [Tooltip("Whether or not stamina should be used")]
        [SerializeField] private bool useStamina = true;


        // For moving the camera
        private float xRotation;

        // Private bools for data
        private bool grounded;
        private bool sprinting;
        private bool crouching;

        private bool isWalking;

        private bool jumpRequest;

        // Create the input actions
        private void Awake() {
            inputActions = new _3DPlayerControllerActions();
        }

        private void Start() {
            // Get the character controller, set the walk speed, and lock the cursor
            thisController = GetComponent<CharacterController>();
            curSpeed = walkSpeed;
            Cursor.lockState = CursorLockMode.Locked;

            stamina = 1f;

            // Set up the inputs
            SetupInputActions();
        }

        // Enable the input actions when the object is enabled
        private void OnEnable() {
            if (inputActions != null)
                inputActions.Enable();
        }

        // Disable the input actions when the object is disabled
        private void OnDisable() {
            if (inputActions != null)
                inputActions.Disable();
        }

        private void Update() {
            // Multiplying speed
            if (sprinting && !crouching && (stamina > 0 || !useStamina) && allowSprint)
                curSpeed = runSpeed * speedMultiplier;
            else if (crouching && !sprinting && allowCrouch)
                curSpeed = crouchSpeed * speedMultiplier;
            else
                curSpeed = walkSpeed * speedMultiplier;

            // Call all movement functions
            Move();
            Look();
            Crouch();

            // Only if the player is sprinting AND walking do we want to decrease their stamina
            if (sprinting && isWalking) {
                // Clamp the sum of stamina minus deltaTime divided by sprint to a minimum of 0 and maximum of 1
                stamina = Mathf.Clamp(stamina - Time.deltaTime / sprintTime, 0f, 1f);
            } else {
                // If they're not walking then increase the stamina faster than if they are
                if (!isWalking) {
                    // Same as above but add 4 to sprintTime, as we're still using it as the increase value
                    stamina = Mathf.Clamp(stamina + Time.deltaTime / (sprintTime + 4f), 0f, 1f);
                } else {
                    // Same as above but add 9 to sprintTime, as we're still using it as the increase value
                    // Added 9 instead of 4 because the player is still moving
                    stamina = Mathf.Clamp(stamina + Time.deltaTime / (sprintTime + 9f), 0f, 1f);
                }
            }

            // If the player hits a wall while jumping, make sure their y velocity is 0 or below.
            if (!grounded && isWalking && Physics.Raycast(transform.position, transform.forward, thisController.radius + 0.1f) && velocity.y > 0) {
                yVelocityUncapped = 0;
                velocity.y = 0;
            }

            // If the camera's FOV doesn't match our target FOV, make it.
            if (cam.fieldOfView != targetFOV)
                cam.fieldOfView = targetFOV;
        }

        private void Move() {
            // Set the grounded variable and make sure the player is grounded if needed
            grounded = thisController.isGrounded;
            if (grounded && velocity.y < 0)
                yVelocityUncapped = -2f;

            // Get the input from the inputActions and turn it into a usable Vector3
            Vector2 moveVector = inputActions.Player.Move.ReadValue<Vector2>();
            Vector3 move = transform.right * moveVector.x + transform.forward * moveVector.y;
            // Move the controller
            thisController.Move(move.normalized * curSpeed * Time.deltaTime);
            isWalking = move != Vector3.zero;

            // Update our y velocity depending on whether or not the player has requested to jump
            if (!jumpRequest)
                yVelocityUncapped = yVelocityUncapped + gravity * Time.deltaTime;
            else {
                yVelocityUncapped = jumpForce;
                jumpRequest = false;
            }
            // Set the velocity and move the character controller using it
            velocity.y = Mathf.Clamp(yVelocityUncapped, -60f, 60f);
            thisController.Move(velocity * Time.deltaTime);
        }

        private void Look() {
            // Get the input from inputActions and store it in separate variables
            Vector2 lookVector = inputActions.Player.Look.ReadValue<Vector2>();
            float lookX = lookVector.x * sensitivity * Time.deltaTime;
            float lookY = lookVector.y * sensitivity * Time.deltaTime;

            // Set the xRotation varaible
            xRotation -= lookY;
            xRotation = Mathf.Clamp(xRotation, -75f, 90f);

            // Rotate the camera up and down, and the whole character left and right
            cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
            transform.Rotate(Vector3.up * lookX);
        }

        private void Crouch() {
            if (!allowCrouch) return;
            // If the crouch button is pressed and they're not crouching
            if (inputActions.Player.Crouch.ReadValue<float>() > 0) {
                if (!crouching) {
                    // Set the controller height
                    thisController.height = crouchHeight;
                    crouching = true;

                    // If the player is sprinting, make them a bit faster than normal crouching
                    if (sprinting)
                        curSpeed = walkSpeed;
                    else
                        curSpeed = crouchSpeed;
                }
            } else {
                // Check if something is above the player and that they're not crouching
                if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.up), 3.0f, notPlayerMask)) {
                    if (!crouching) {
                        // Set the controller height
                        thisController.height = crouchHeight;
                        crouching = true;

                        // If the player is sprinting, make them a bit faster than normal crouching
                        if (sprinting)
                            curSpeed = walkSpeed;
                        else
                            curSpeed = crouchSpeed;
                    }
                } else if (crouching) {
                    // Set the controller height
                    thisController.height = baseHeight;
                    crouching = false;

                    // If sprinting, return to normal sprint speed, else just walk
                    if (sprinting)
                        curSpeed = runSpeed;
                    else
                        curSpeed = walkSpeed;
                }
            }
        }

        private void SetupInputActions() {
            // Sprint and unsprint
            inputActions.Player.Sprint.performed += ctx => {
                if (!allowSprint) return;
                curSpeed = (crouching ? walkSpeed : runSpeed);
                sprinting = true;
            };
            inputActions.Player.Sprint.canceled += ctx => {
                curSpeed = (crouching ? crouchSpeed : runSpeed);
                sprinting = false;
            };

            // Jump
            inputActions.Player.Jump.performed += ctx => {
                if (!allowJump) return;
                if (grounded && !crouching && (stamina >= jumpStaminaTax || !useStamina)) {
                    jumpRequest = true;
                    stamina -= jumpStaminaTax;
                }
            };
        }

        public float getCurrentStamina() => stamina;
    }
}
