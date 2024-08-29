using System;
using System.Collections.Generic;
using System.Text;
using Urho3DNet;
using ZombieDollsCS.GameStates;

namespace ZombieDollsCS
{
    public partial class MDScene : Component
    {
        public UrhoPluginApplication _app;
        public GameState _gameState;
        public SharedPtr<Scene> _scene;
        private readonly Context _context;
        private readonly Node _cameraNode;
        public readonly Camera _camera;
        public SharedPtr<Node> _zombiesNode;

        public BoundingBox _bounds = new BoundingBox(new Vector3(-20.0f, 0.0f, -15.0f), new Vector3(20.0f, 0.0f, 20.0f));

        private readonly Random _random = new Random();

        public MDScene(Context context, UrhoPluginApplication app, GameState gameState) : base(context)
        { 
            _context = context;
            _app = app;
            _gameState = gameState;

            _scene = _context.CreateObject<Scene>();
            //_scene.Ptr.LoadXML("Scenes/Scene.scene");

            _scene.Ptr.CreateComponent<Octree>();
            _scene.Ptr.CreateComponent<PhysicsWorld>();
            _scene.Ptr.CreateComponent<DebugRenderer>();

            // Create a Zone component for ambient lighting & fog control
            Node zoneNode = _scene.Ptr.CreateChild("Zone");
            var zone = zoneNode.CreateComponent<Zone>();
            zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
            zone.AmbientColor = new Color(0.1f, 0.0f, 0.0f);
            zone.FogColor = new Color(0.5f, 0.5f, 0.7f);
            zone.FogStart = 50.0f;
            zone.FogEnd = 300.0f;

            // Create a directional light to the world. Enable cascaded shadows on it
            var lightNode = _scene.Ptr.CreateChild("DirectionalLight");
            lightNode.Direction = new Vector3(0.6f, -1.0f, 0.8f);
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.LightDirectional;
            light.CastShadows = true;
            light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
            // Set cascade splits at 10, 50 and 200 world units, fade shadows out at 80% of maximum shadow distance
            light.ShadowCascade = new CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f);

            // Create skybox. The Skybox component is used like StaticModel, but it will be always located at the camera, giving the
            // illusion of the box planes being far away. Use just the ordinary Box model and a suitable material, whose shader will
            // generate the necessary 3D texture coordinates for cube mapping
            var skyNode = _scene.Ptr.CreateChild("Sky");
            skyNode.Scale(500.0f); // The scale actually does not matter
            var skybox = skyNode.CreateComponent<Skybox>();

            var cache = _context.GetSubsystem<ResourceCache>();
            skybox.SetModel(cache.GetResource<Model>("Models/Box.mdl"));
            skybox.SetMaterial(cache.GetResource<Material>("Materials/Skybox.xml"));

            // Create a floor object, 500 x 500 world units. Adjust position so that the ground is at zero Y
            var floorNode = _scene.Ptr.CreateChild("Floor");
            floorNode.Position = new Vector3(0.0f, -0.5f, 0.0f);
            floorNode.Scale(new Vector3(500.0f, 1.0f, 500.0f));
            var floorObject = floorNode.CreateComponent<StaticModel>();
            floorObject.SetModel(cache.GetResource<Model>("Models/Box.mdl"));
            floorObject.SetMaterial(cache.GetResource<Material>("Materials/StoneTiled.xml"));

            // Make the floor physical by adding RigidBody and CollisionShape components
            var body = floorNode.CreateComponent<RigidBody>();
            // We will be spawning spherical objects in this sample. The ground also needs non-zero rolling friction so that
            // the spheres will eventually come to rest
            body.RollingFriction = 0.15f;
            var shape = floorNode.CreateComponent<CollisionShape>();
            // Set a box shape of size 1 x 1 x 1 for collision. The shape will be scaled with the scene node scale, so the
            // rendering and physics representation sizes should match (the box model is also 1 x 1 x 1.)
            shape.SetBox(Vector3.One);

            _cameraNode = _scene.Ptr.CreateChild("Camera Node");

            var shakeComponent = _cameraNode.CreateComponent<ShakeComponent>();
            shakeComponent.TraumaPower = 1.0f;
            shakeComponent.TraumaFalloff = 2.0f;
            shakeComponent.TimeScale = 10.0f;
            shakeComponent.ShiftRange = new Vector3(0.0f, 0.5f, 0.0f);
            shakeComponent.RotationRange = new Vector3(0.0f, 0.5f, 0.0f);

            _cameraNode.CreateComponent<FreeFlyController>();
            _camera = _cameraNode.CreateComponent<Camera>();
            _camera.FarClip = 300.0f;

            // Set an initial position for the camera scene node above the floor
            _cameraNode.Position = new Vector3(0.0f, 2.0f, -20.0f);
            var q = new Quaternion(-90.0f, 90.0f, 90.0f);

            _scene.Ptr.IsUpdateEnabled = false;

            var gunNode = _cameraNode.CreateChild("Gun Node");
            gunNode.Position = new Vector3(0.0f, -0.2f, 0.5f);
            var model = gunNode.CreateComponent<StaticModel>();
            model.SetModel(cache.GetResource<Model>("Models/ar15.mdl"));
            model.CastShadows = true;

            gunNode.Rotation = q;
            gunNode.SetScale(25.0f);

            var shapeNode = _cameraNode.CreateChild("Shape Node");
            shapeNode.Position = new Vector3(0.1f, -0.4f, 30.0f);
            var model2 = shapeNode.CreateComponent<StaticModel>();
            model2.SetModel(cache.GetResource<Model>("Models/Cylinder.mdl"));
            model2.SetMaterial(new Material(_context));
            model2.GetMaterial().SetShaderParameter("MatEmissiveColor", new Color(1, 0, 0));
            model2.CastShadows = true;
            shapeNode.Rotation = q;
            shapeNode.SetScale(new Vector3(0.05f, 50.0f, 0.05f));

            // Subscribe HandlePostRenderUpdate() function for processing the post-render update event, during which we request
            // debug geometry
            SubscribeToEvent(E.PostRenderUpdate, HandlePostRenderUpdate);
        }

        public void CreateKicking(Node ptr)
        {
            var cache = _context.GetSubsystem<ResourceCache>();
            Model model_running = cache.GetResource<Model>("Models/Ch36.mdl");
            Animation animation_running = cache.GetResource<Animation>("Models/MeleeAttack.ani");

            var modelObject = ptr.GetComponent<AnimatedModel>();
            modelObject.SetModel(model_running);

            var animationController = ptr.GetComponent<AnimationController>();
            animationController.PlayNewExclusive(new AnimationParameters(animation_running).Looped().Time(0));
        }

        public void CreateModels(int dollsNum, float dollsSpeed)
        {
            var cache = _context.GetSubsystem<ResourceCache>();

            if (_zombiesNode == null)
                _zombiesNode = _scene.Ptr.CreateChild("Zombie");
            else
                _zombiesNode.Ptr.RemoveAllChildren();

            Random rnd = new Random();
            for (int x = -dollsNum / 2, i = 0; i < dollsNum; ++x, i++)
            {
                string name = "Zombie_" + i.ToString();
                Node modelNode = _zombiesNode.Ptr.CreateChild(name);

                float X = x * 4.0f;
                float Y = 14 + rnd.Next(6) / 7.0f;
                float phi = (float)Math.Atan2((double)X, (double)Y);

                modelNode.Position = new Vector3(X, 0.0f, Y);
                modelNode.Rotation = new Quaternion(0.0f, 180.0f * (1.0f + 0.4f * phi / (float)Math.PI), 0.0f);

                var modelObject = modelNode.CreateComponent<AnimatedModel>();
                //Model* model_running = cache->GetResource<Model>("Models/Zombie Running.fbx.d/Models/Ch10.mdl");
                Model model_running = cache.GetResource<Model>("Models/Jack.mdl");
                modelObject.SetModel(model_running);
                modelObject.CastShadows = true;
                // Set the model to also update when invisible to avoid staying invisible when the model should come into
                // view, but does not as the bounding box is not updated
                modelObject.UpdateInvisible = true;

                // Create a rigid body and a collision shape. These will act as a trigger for transforming the
                // model into a ragdoll when hit by a moving object
                var body = modelNode.CreateComponent<RigidBody>();
                // The Trigger mode makes the rigid body only detect collisions, but impart no forces on the
                // colliding objects
                body.IsTrigger = true;
                var shape = modelNode.CreateComponent<CollisionShape>();
                // Create the capsule shape with an offset so that it is correctly aligned with the model, which
                // has its origin at the feet
                shape.SetCapsule(0.7f, 2.0f, new Vector3(0.0f, 1.0f, 0.0f));

                // Create an AnimationState for a walk animation. Its time position will need to be manually updated to advance the
                // animation, The alternative would be to use an AnimationController component which updates the animation automatically,
                // but we need to update the model's position manually in any case
                var animation_running = cache.GetResource<Animation>("Models/Jack_Walk.ani");
                //Animation* animation_running = cache->GetResource<Animation>("Models/Zombie Running.fbx.d/Animations/mixamo.com.ani");
                int startTime = rnd.Next((int)animation_running.Length);
                var animationController = modelNode.CreateComponent<AnimationController>();
                animationController.PlayNewExclusive(new AnimationParameters(animation_running).Looped().Time(startTime));

                // Create our custom Mover3D component that will move & animate the model during each frame's update
                Vector3 v = new Vector3(dollsSpeed * (float)Math.Tan(phi) * 0.1f, 0, dollsSpeed);
                modelNode.CreateComponent<Mover3D>().SetGameState(_gameState).SetSpeed(v);

                // Create a custom component that reacts to collisions and creates the ragdoll
                modelNode.CreateComponent<CreateRagdoll>().SetGameState(_gameState);
            }
        }

        void HandlePostRenderUpdate(StringHash eventType, VariantMap eventData)
        {
            // If draw debug mode is enabled, draw physics debug geometry. Use depth test to make the result easier to interpret
            //_scene.Ptr.GetComponent<PhysicsWorld>().DrawDebugGeometry(true);
        }

        public void SpawnObject()
        {
            var cache = _context.GetSubsystem<ResourceCache>();

            Node boxNode = _scene.Ptr.CreateChild("Sphere");
            boxNode.Position = _cameraNode.Position;
            boxNode.Rotation = _cameraNode.Rotation;
            boxNode.SetScale(0.25f);
            var boxObject = boxNode.CreateComponent<StaticModel>();
            boxObject.SetModel(cache.GetResource<Model>("Models/Sphere.mdl"));
            boxObject.SetMaterial(cache.GetResource<Material>("Materials/StoneSmall.xml"));
            boxObject.CastShadows = true;

            var body = boxNode.CreateComponent<RigidBody>();
            body.Mass = 1.0f;
            body.RollingFriction = 0.15f;
            var shape = boxNode.CreateComponent<CollisionShape>();
            shape.SetSphere(1.0f);

            const float OBJECT_VELOCITY = 20.0f;

            // Set initial velocity for the RigidBody based on camera forward vector. Add also a slight up component
            // to overcome gravity better
            body.LinearVelocity = _cameraNode.Rotation * (new Vector3(0.0f, 0.0f, 7.0f)) * OBJECT_VELOCITY;

            _gameState.PlaySoundEffect("SmallExplosion.wav");

            var shakeComponent = _cameraNode.GetComponent<ShakeComponent>();
            shakeComponent.AddTrauma(1.0f);
        }
    }
}
