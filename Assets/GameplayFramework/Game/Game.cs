﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameplayFramework
{
    public class Game
    {
        #region Singleton

        private static readonly object _instanceLock = new object();
        private static Anchor _anchor;
        private static Game _instance;

        private Game()
        {
            lock(_instanceLock)
            {
                if(_instance != null)
                    throw new InvalidOperationException("The Game has already been initialized.");

                _instance = this;
            }
        }



        public static Game Current
        {
            get
            {
                return _instance;
            }
        }



        public static void Initialize(Anchor anchor)
        {
            if(anchor == null)
                throw new ArgumentNullException("anchor");

            _anchor = anchor;

            new Game();
            anchor.Tick += (sender, e) => _instance.Tick();
        }

        #endregion

        #region GameMode and GameState

        private GameMode _gameMode;
        private GameState _gameState;

        public GameMode GameMode
        {
            get
            {
                return _gameMode;
            }
        }

        public GameState GameState
        {
            get
            {
                return _gameState;
            }
        }



        public void SetGameMode(GameModeName gameMode)
        {
            string gameModeName = Enum.GetName(typeof(GameModeName), gameMode);

            Type type;

            // Get the type of game mode.
            {
                Type[] types = Assembly.GetExecutingAssembly().GetTypes();
                List<Type> matchingTypes = types.Where(t => t.Name == gameModeName).ToList();

                if(matchingTypes.Count() == 1)
                {
                    type = matchingTypes[0];
                }
                else if(matchingTypes.Count() == 0)
                {
                    throw new InvalidOperationException("Couldn't find a type with name '" + gameModeName + "'.");
                }
                else
                {
                    throw new InvalidOperationException("Found multiple types with name '" + gameModeName + "'.");
                }
            }

            object instance;

            // Create an instance of the game mode.
            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException("Couldn't create an instance of the type with name '" + gameModeName + "'.", ex);
            }

            // Make sure the instance is a game mode.
            if(instance is GameMode)
            {
                SetGameMode((GameMode)instance);
            }
            else
            {
                throw new InvalidOperationException("The type with name '" + gameModeName + "' is not a '" + typeof(GameMode).Name + "'.");
            }
        }



        public void SetGameMode<T>() where T : GameMode, new()
        {
            SetGameMode(new T());
        }



        private void SetGameMode(GameMode mode)
        {
            GameMode oldMode = _gameMode;

            _gameMode = mode;
            _gameMode.Initialize(_anchor);

            if(oldMode != null)
                oldMode.EndMode();

            _gameMode.BeginMode();
        }

        #endregion

        #region Scene loading

        private readonly object _sceneLock = new object();

        private AsyncOperation _sceneLoader;


        public event EventHandler PreLoadScene;
        public event EventHandler PostLoadScene;



        public virtual void LoadScene(SceneName scene)
        {
            lock(_sceneLock)
            {
                if(_sceneLoader != null)
                    throw new InvalidOperationException("Only a single scene can be loaded at a time.");

                string sceneName = Enum.GetName(typeof(SceneName), scene);

                var preLoadScene = PreLoadScene;
                if(preLoadScene != null)
                    preLoadScene(null, EventArgs.Empty);
                
                _sceneLoader = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
        }

        public virtual void Tick()
        {
            AsyncOperation sceneLoader = _sceneLoader;

            if(sceneLoader != null && sceneLoader.isDone)
            {
                lock(_sceneLock)
                {
                    _sceneLoader = null;

                    var postLoadScene = PostLoadScene;
                    if(postLoadScene != null)
                        postLoadScene(null, EventArgs.Empty);
                }
            }
        }

        #endregion
    }
}