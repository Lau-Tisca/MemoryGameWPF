# Memory Game WPF

Acesta este un proiect implementat în C# și WPF, care dezvoltă un joc clasic de Memory. Aplicația implementează conceptele de Data Binding și arhitectura MVVM.

## Descriere Generală

Jocul Memory permite utilizatorilor să își creeze conturi, să asocieze o imagine de profil și apoi să joace jocul de potrivire a perechilor de imagini. Aplicația include funcționalități de salvare și încărcare a jocurilor, statistici ale jucătorilor și un cronometru pentru a adăuga o provocare.

## Funcționalități Implementate

### 1. Intrarea în Sistem (Sign In)
*   **Creare Utilizator Nou:** Permite crearea unui cont nou cu un nume de utilizator și asocierea unei imagini de profil (jpg, png, gif).
*   **Selectare Utilizator Existent:** Afișează o listă a utilizatorilor existenți și imaginea de profil la selectare.
*   **Ștergere Utilizator:** Permite ștergerea unui cont, inclusiv datele asociate (imagine, jocuri salvate, statistici).
*   **Persistența Utilizatorilor:** Detaliile utilizatorilor sunt salvate local într-un fișier `users.json`.
*   **Butoane Dinamice:** Butoanele "Play" și "Delete User" sunt active doar când un utilizator este selectat.

### 2. Jocul
*   **Meniu Principal:**
    *   **File:** Category (selectare categorie imagini), New Game, Open Game, Save Game, Statistics, Exit (revenire la Sign In).
    *   **Options:** Standard (4x4). _(Opțiunea Custom este planificată)._
    *   **Help:** About (informații student).
*   **Logica Jocului:**
    *   Generare aleatorie a tablei la "New Game".
    *   Mecanism de întoarcere și potrivire a perechilor de cărți.
    *   Condiții de câștig (toate perechile găsite) și pierdere (timp expirat).
*   **Timer:** Timp limită pentru finalizarea jocului, afișat în interfață.

### 3. Salvarea și Deschiderea Jocului
*   Salvarea stării curente a jocului (categorie, tablă, timp) într-un fișier `.mgs` (format JSON).
*   Încărcarea unui joc salvat anterior de utilizatorul curent.
*   Ștergerea automată a fișierului de salvare la finalizarea unui joc încărcat.

### 4. Ștergerea unui Utilizator
*   Include ștergerea datelor din `users.json`, a imaginii de profil, a jocurilor salvate și a statisticilor din `game_stats.json`.

### 5. Salvarea și Vizualizarea Statisticilor
*   Actualizarea automată a numărului de jocuri jucate/câștigate la finalul fiecărui joc.
*   Stocarea statisticilor în `game_stats.json`.
*   Afișarea statisticilor pentru toți jucătorii (Nume, Jocuri Jucate, Jocuri Câștigate), sortate.

## Tehnologii și Concepte Utilizate
*   **Limbaj:** C#
*   **Framework UI:** WPF (Windows Presentation Foundation)
*   **Model Arhitectural:** MVVM (Model-View-ViewModel)
*   **Data Binding:** Utilizat extensiv pentru legătura dintre UI (View) și logică (ViewModel).
*   **Commands (`ICommand`):** Implementate prin `RelayCommand` pentru gestionarea acțiunilor utilizatorului.
*   **`INotifyPropertyChanged`:** Pentru notificarea UI-ului despre schimbările de date în ViewModels.
*   **`ObservableCollection<T>`:** Pentru actualizări automate ale UI-ului la modificarea colecțiilor (utilizatori, cărți).
*   **Serializare JSON (`System.Text.Json`):** Pentru persistența datelor (utilizatori, stări joc, statistici).
*   **Controale WPF:** `ItemsControl` cu `UniformGrid` pentru tabla de joc, `Menu`, `Button`, `TextBlock`, `Image`, `ListBox`.
*   **Stilizare și Template-uri:** `DataTemplate`, `Style`, `DataTriggers` pentru aspectul și comportamentul vizual al cărților.
*   **`DispatcherTimer`:** Pentru cronometrul jocului.
*   **Dialoguri Standard:** `OpenFileDialog`, `SaveFileDialog`.
*   **Imagini ca Resurse:** Pentru încorporarea imaginilor jocului direct în aplicație.

## Cum se Rulează

1.  Clonați repository-ul (dacă este cazul).
2.  Deschideți soluția (`.sln`) în Visual Studio.
3.  Asigurați-vă că aveți .NET Framework (versiunea specificată în proiect) instalat.
4.  Construiți soluția (Build -> Build Solution).
5.  Rulați aplicația (Debug -> Start Debugging sau F5).
    *   Fișierele de date (`users.json`, `game_stats.json`, jocurile salvate `.mgs`) vor fi generate automat în directorul de output (ex: `bin\Debug`) la prima rulare/salvare.

## Posibile Îmbunătățiri / Funcționalități Neimplementate
*   Implementarea completă a opțiunii "Custom" pentru dimensiunea tablei de joc.
*   Animații la întoarcerea cărților.
*   Sunete pentru acțiuni.
*   Salvarea robustă a imaginii avatarului (copierea imaginii în folderul aplicației).
*   Niveluri de dificultate.
*   Un sistem de scor mai complex.

---

*Acest proiect a fost realizat de Tișcă Laurențiu-Ștefan pentru Tema 2 la MVP (Medii Vizuale de Programare).*
