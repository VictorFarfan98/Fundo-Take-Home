const express = require("express");
const app = express();
const applications = new Map();

app.use(express.json());
app.post("/applications", (req, res) => {
  applications.set(req.body.Application.Id, req.body);
  res.sendStatus(204);
});
app.put("/applications/:id", (req, res) => {
  applications.set(req.params.id, req.body);
  res.sendStatus(204);
});
app.get("/applications", (_, res) => res.json([...applications.values()]));
app.listen(3001, () => console.log("Mock service listening on 3001"));
