## Mejoras al RF_01 - Alta de socio

1. **Agregar un campo en socio Tipo de Documento:** Se quiere agregar un enum que permita seleccionar el tipo de documento: CI, Pasaporte u Otro. El campo es obligatorio (no puede registrarse un socio sin especificar el tipo de documento).

2. **Agregrar validación:** En caso de que en el enum anterior se use CI se debe validar que sea una cédula (uruguaya) válida

3. **Agregar validación:** No pueden existir socios con cédulas repetidas

