using System;

public interface ICommunicator
{
    /// <summary>
    /// Round:来回，Trip:旅途
    /// 就是ping时间
    /// </summary>
    /// <returns></returns>
	int RoundTripTime();

	void OpRaiseEvent(byte eventCode, object message, bool reliable, int[] toPlayers);

	void AddEventListener(OnEventReceived onEventReceived);
}
