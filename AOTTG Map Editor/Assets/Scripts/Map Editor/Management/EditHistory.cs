using System.Collections.Generic;
using UnityEngine;

namespace MapEditor
{
    //An abstract class that defines functions to do and undo edits
    public abstract class EditCommand
    {
        public abstract void ExecuteEdit();
        public abstract void RevertEdit();
    }

    //Keeps a history of the executed commands and enables undo/redo
    public class EditHistory : MonoBehaviour
    {
        //A self-reference to the singleton instance of this script
        public static EditHistory Instance { get; private set; }
        //Stores the commands that were executed and reverted
        private Stack<EditCommand> executedCommands;
        private Stack<EditCommand> revertedCommands;

        void Awake()
        {
            //Set this script as the only instance of the EditorManger script
            if (Instance == null)
                Instance = this;

            //Initialize the stacks
            executedCommands = new Stack<EditCommand>();
            revertedCommands = new Stack<EditCommand>();
        }

        private void Update()
        {
            //Check for undo & redo shortcuts
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    //If ctrl + z is pressed without the shift key, undo
                    if (!Input.GetKey(KeyCode.LeftShift))
                        Instance.Undo();
                    //If the shift key is also held, redo
                    else
                        Instance.Redo();
                }
                else if (Input.GetKeyDown(KeyCode.Y))
                    Instance.Redo();
            }
        }

        //Add a command to the history
        public void AddCommand(EditCommand newCommand)
        {
            executedCommands.Push(newCommand);
            revertedCommands.Clear();
        }

        //Clear the history of executed and reverted commands
        public void ResetHistory()
        {
            executedCommands = new Stack<EditCommand>();
            revertedCommands = new Stack<EditCommand>();
        }

        //Undo the changes made in the last command
        public void Undo()
        {
            //If no commands have been executed yet, return
            if (executedCommands.Count == 0)
                return;

            EditCommand lastExecuted = executedCommands.Pop();
            lastExecuted.RevertEdit();
            revertedCommands.Push(lastExecuted);
        }

        //Reapply the changes that were last reverted
        public void Redo()
        {
            //If no commands were reverted, return
            if (revertedCommands.Count == 0)
                return;

            EditCommand lastReverted = revertedCommands.Pop();
            lastReverted.ExecuteEdit();
            executedCommands.Push(lastReverted);
        }
    }
}