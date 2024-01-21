using System;

public interface IHasProgress {
    public event EventHandler<onProgressChangedEventArgs> onProgressChanged;
    public class onProgressChangedEventArgs: EventArgs {
        public float progressNormalized;
    }
}
