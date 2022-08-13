/// DelayedActions script - simplified and debuggable alternative to Invoke,
/// InvokeRepeating and Coroutines.
/// 
/// Copyright © 2017-2022 Virtual Play
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualPlay.DelayedActions
{
    public class DelayedAction
    {
        public Action Action;
        public float Interval;
        public float Remaining;
        public int TimesRun { get; internal set; }
        public int Repeats;
        public bool Unscaled;
        public bool Paused;
        public bool Stopped { get; internal set; }

        /// <summary>
        /// Stop the delayed action.
        /// The action is removed, and can only be resumed using <see cref="Restart"/>.
        /// </summary>
        public bool Stop() => DelayedActions.Stop(this);
        /// <summary>
        /// Stop the delayed action.
        /// </summary>
        public bool Restart() => DelayedActions.Restart(this);
        /// <summary>
        /// Resets the timer on the delayed action.
        /// </summary>
        public void Reset() => Remaining = Interval;
        /// <summary>
        /// Pauses the delay for the delayed action.
        /// </summary>
        public void Pause() => Paused = true;
        /// <summary>
        /// Resumes the delay for the delayed action.
        /// </summary>
        public void Resume() => Paused = false;
    }

    /// <summary>
    /// Control class responsible for the invocation of registered DelayedActions.
    /// </summary>
    public class DelayedActions : MonoBehaviour
    {
        static DelayedActions instance;

        public static bool DestroyOnLoad = false;

        List<DelayedAction> actions;
        List<Action> nextUpdateActions;
        List<Action> nextFixedUpdateActions;

        static bool quitting;

        void Awake()
        {
            if (instance != null && instance != this) Debug.LogError($"Multiple DelayedActions components detected in the scene!\nThere should only be one DelayedActions component at all time.", gameObject);

            actions = new List<DelayedAction>();
            nextUpdateActions = new List<Action>();
            nextFixedUpdateActions = new List<Action>();

            Application.quitting += Quitting;
        }

        void Quitting()
        {
            quitting = true;
        }

        static void Init()
        {
            if (instance == null && !quitting)
            {
                instance = new GameObject("Delayed Actions Scheduler", typeof(DelayedActions)).GetComponent<DelayedActions>();
                if (!DestroyOnLoad) DontDestroyOnLoad(instance.gameObject);
            }
        }

        /// <summary>
        /// Executes <paramref name="action"/> after <paramref name="delay"/> seconds.
        /// </summary>
        /// <param name="action">The code to execute.</param>
        /// <param name="delay">Number of seconds to wait.</param>
        /// <param name="numberOfTimes">Number of times to repeat the execution. Pass 0 to repeat indefinitely.</param>
        /// <param name="unscaled">Use unscaled time, ignoring e.g. pausing by <code>Time.timeScale = 0f</code>.</param>
        /// <returns>The <see cref="DelayedAction"/> object that can be used to pause, reset or stop the delayed action.</returns>
        public static DelayedAction Start(Action action, float delay, int numberOfTimes = 1, bool unscaled = false)
        {
            Init();

            DelayedAction newAction = new() { Action = action, Interval = delay, Remaining = delay, Repeats = numberOfTimes, Unscaled = unscaled };
            instance.actions.Add(newAction);

            return newAction;
        }

        /// <summary>
        /// Executes <paramref name="action"/> at next Update.
        /// </summary>
        /// <param name="action">The code to execute.</param>
        public static void NextUpdate(Action action)
        {
            Init();

            lock(instance.nextUpdateActions)
            {
                instance.nextUpdateActions.Add(action);
            }
        }

        /// <summary>
        /// Executes <paramref name="action"/> at next FixedUpdate.
        /// </summary>
        /// <param name="action">The code to execute.</param>
        public static void NextFixedUpdate(Action action)
        {
            Init();

            lock (instance.nextFixedUpdateActions)
            {
                instance.nextFixedUpdateActions.Add(action);
            }
        }

        /// <summary>
        /// Stop delayed action <paramref name="actionToStop"/>.
        /// The action is removed, and can only be resumed using <see cref="Restart"/>.
        /// </summary>
        /// <param name="actionToStop">The action to stop.</param>
        public static bool Stop(DelayedAction actionToStop)
        {
            Init();

            if (actionToStop == null || actionToStop.Stopped) return false;
            if (instance.actions.Remove(actionToStop))
            {
                actionToStop.Stopped = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Restart previously stopped delayed action <paramref name="actionToRestart"/>.
        /// </summary>
        /// <param name="actionToRestart">The action to stop.</param>
        public static bool Restart(DelayedAction actionToRestart)
        {
            Init();

            if (actionToRestart == null || !actionToRestart.Stopped) return false;

            actionToRestart.Reset();
            instance.actions.Add(actionToRestart);

            return true;
        }


        /// <summary>
        /// Resets the timer on delayed action <paramref name="actionToReset"/>.
        /// </summary>
        /// <param name="actionToReset">The action to reset.</param>
        public static void Reset(DelayedAction actionToReset) => actionToReset.Reset();

        /// <summary>
        /// Pauses the delay for action <paramref name="actionToPause"/>.
        /// </summary>
        /// <param name="actionToPause">The action to pause.</param>
        public static void Pause(DelayedAction actionToPause) => actionToPause.Pause();

        /// <summary>
        /// Resumes the delay for action <paramref name="actionToResume"/>.
        /// </summary>
        /// <param name="actionToResume">The action to resume.</param>
        public static void Resume(DelayedAction actionToResume) => actionToResume.Resume();

        int fixedCounter;
        void FixedUpdate()
        {
            lock(nextFixedUpdateActions)
            {
                for (fixedCounter = 0; fixedCounter < nextFixedUpdateActions.Count; fixedCounter++)
                {
                    try
                    {
                        nextFixedUpdateActions[fixedCounter].Invoke();
                    }
                    catch (Exception e)
                    {
#if UNITY_EDITOR
                        Debug.LogWarning($"DelayedAction caught an Exception: {e}", this);
#endif
                    }
                }
                nextFixedUpdateActions.Clear();
            }
        }

        int actionCounter;
        DelayedAction actionToRun;
        void Update()
        {
            for (actionCounter = actions.Count - 1; actionCounter >= 0; actionCounter--)
            {
                actionToRun = actions[actionCounter];
                if (!actionToRun.Paused && actionToRun.Remaining > 0f)
                {
                    actionToRun.TimesRun++;
                    actionToRun.Remaining -= actionToRun.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                    if (actionToRun.Remaining <= 0f)
                    {
                        actionToRun.Remaining = (actionToRun.Repeats == 0 || actionToRun.Repeats > actionToRun.TimesRun) ? (actionToRun.Remaining + actionToRun.Interval) : 0f;
                        try
                        {
                            actionToRun.Action.Invoke();
                        }
                        catch (Exception e)
                        {
#if UNITY_EDITOR
                            Debug.LogWarning($"DelayedAction caught an Exception: {e}", this);
#endif
                            actions.RemoveAt(actionCounter);
                        }
                    }
                }
            }

            lock(nextUpdateActions)
            {
                for (actionCounter = 0; actionCounter < nextUpdateActions.Count; actionCounter++)
                {
                    try
                    {
                        nextUpdateActions[actionCounter].Invoke();
                    }
                    catch (Exception e)
                    {
#if UNITY_EDITOR
                        Debug.LogWarning($"DelayedAction caught an Exception: {e}", this);
#endif
                    }
                }
                nextUpdateActions.Clear();
            }
        }
    }
}