namespace JegorowordleHeroes.Models
{
    public enum LetterResult
    {
        Correct,        // Buchstabe + Position richtig
        WrongPosition,  // Buchstabe drin, aber falsche Position
        NotInWord       // Buchstabe nicht im Zielwort
    }

}
