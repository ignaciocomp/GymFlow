#diagrama 

```plantuml

@startuml metodologia-trabajo
title Metodología de Trabajo - GymFlow
skinparam activityShape octagon
skinparam shadowing false
skinparam defaultFontName Arial
skinparam ActivityBackgroundColor #E8F4FD
skinparam ActivityBorderColor #2E75B6
skinparam ActivityDiamondBackgroundColor #FFF3CD
skinparam ActivityDiamondBorderColor #D4A017
skinparam NoteBackgroundColor #F0F0F0

|#E8F4FD|Anteproyecto|
start
:Definir lógica de negocio
y requerimientos del cliente;
note right
  Casos de uso, RF, RNF
  Stack tecnológico
  Cronograma
end note
:Redactar y entregar
anteproyecto;

|#D5F5E3|Iteración N|
repeat

  repeat
    :Seleccionar RF/RNF
    del cronograma para
    la iteración;

    partition "Desarrollo (Spec-Driven con Claude)" {
      :Escribir **spec** del
      requerimiento;
      note right
        En vault Obsidian.
        Diseño detallado:
        entidades, endpoints,
        lógica, validaciones
      end note
      :Escribir **plan** de
      implementación;
      note right
        Pasos concretos,
        archivos a modificar,
        orden de ejecución
      end note
      :Implementar siguiendo
      el plan;
      :Integrar a **develop**
      vía Pull Request;
      note right
        feature/* → develop
        Revisión por pares
        Ver [[diagrama_git_pipelines]]
      end note
    }

  repeat while (¿Más RF/RNF en\nesta iteración?) is (sí) not (no)

  partition "Testing" {
    :Elaborar plan de pruebas;
    :Ejecutar pruebas de API
    con Postman;
    note right
      Happy path, errores
      de validación, permisos,
      autenticación, tiempos
    end note
    :Ejecutar pruebas
    funcionales de frontend;
    note right
      Capturas de pantalla
      por cada prueba
    end note
  }

  :Merge **develop → main**
  al cierre de iteración;
  note right
    Versión estable etiquetada
    (tag vX.Y)
    Ver [[diagrama_git_pipelines]]
  end note

  partition "Documentación y entrega" {
    :Escribir documentación
    de la iteración en Markdown
    (Obsidian);
    note right
      Usando template + Claude
      desde specs y código
    end note
    :Exportar a .docx
    con Pandoc;
    :Subir al drive de Teams;
  }

  |#FFF3CD|Reunión con cliente (periódica)|
  floating note
    Las reuniones se agendan
    según disponibilidad y pueden
    cubrir más de una iteración.
  end note
  :Presentar avance al cliente;
  note right
    Demo de funcionalidades,
    recoger feedback y
    ajustes solicitados
  end note
  :Documentar feedback
  del cliente;

  |#D5F5E3|Iteración N|
  :Incorporar feedback
  del cliente al backlog;

repeat while (¿Quedan iteraciones\nen el cronograma?) is (sí) not (no)

|#E8D5F5|Cierre|
:Entrega final del proyecto;
stop

@enduml

````

**Recomendaciones:** 
Exportar con pandoc para mejorar visualización.

**Referencias:**
Diagrama git pipelines [[diagrama_git_pipelines]]]