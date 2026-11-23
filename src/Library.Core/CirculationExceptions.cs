namespace Library.Core;

public class LoanConflictException(string message) : InvalidOperationException(message);

public class ReservationConflictException(string message) : InvalidOperationException(message);

public class CirculationConcurrencyException(string message, Exception innerException)
    : InvalidOperationException(message, innerException);
